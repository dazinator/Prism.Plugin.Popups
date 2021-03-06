﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Prism.Common;
using Prism.Logging;
using Prism.Navigation;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Pages;
using Xamarin.Forms;

namespace Prism.Plugin.Popups
{
    public abstract class PopupPageNavigationServiceBase : PageNavigationService
    {
        protected IPopupNavigation _popupNavigation { get; }

        public PopupPageNavigationServiceBase( IPopupNavigation popupNavigation, IApplicationProvider applicationProvider, ILoggerFacade logger ) 
            : base( applicationProvider, logger )
        {
            _popupNavigation = popupNavigation;
        }

        public override async Task<bool> GoBackAsync(NavigationParameters parameters = null, bool? useModalNavigation = default(bool?), bool animated = true)
        {
            try
            {
                NavigationSource = PageNavigationSource.NavigationService;

                switch(PopupUtilities.TopPage(_popupNavigation, _applicationProvider))
                {
                    case PopupPage popupPage:
                        var segmentParameters = UriParsingHelper.GetSegmentParameters(null, parameters);
                        segmentParameters.Add(KnownNavigationParameters.NavigationMode, NavigationMode.Back);
                        var previousPage = PopupUtilities.GetOnNavigatedToTarget(_popupNavigation, _applicationProvider);

                        PageUtilities.OnNavigatingTo(previousPage, segmentParameters);
                        await DoPop(popupPage.Navigation, false, animated);

                        if(popupPage != null)
                        {
                            PageUtilities.InvokeViewAndViewModelAction<IActiveAware>(popupPage, a => a.IsActive = false);
                            PageUtilities.OnNavigatedFrom(popupPage, segmentParameters);
                            PageUtilities.OnNavigatedTo(previousPage, segmentParameters);
                            PageUtilities.InvokeViewAndViewModelAction<IActiveAware>(previousPage, a => a.IsActive = true);
                            PageUtilities.DestroyPage(popupPage);
                            return true;
                        }
                        break;
                    default:
                        return await base.GoBackAsync(parameters, useModalNavigation, animated);
                }
            }
            catch(Exception e)
            {
                _logger.Log(e.ToString(), Category.Exception, Priority.High);
                return false;
            }
            finally
            {
                NavigationSource = PageNavigationSource.Device;
            }

            return false;
        }

        protected override async Task<Page> DoPop(INavigation navigation, bool useModalNavigation, bool animated)
        {
            if(_popupNavigation.PopupStack.Count > 0)
            {
                await _popupNavigation.PopAsync(animated);
                return null;
            }

            return await base.DoPop(navigation, useModalNavigation, animated);
        }

        protected override Task DoPush(Page currentPage, Page page, bool? useModalNavigation, bool animated, bool insertBeforeLast = false, int navigationOffset = 0)
        {
            switch(page)
            {
                case PopupPage popup:
                    return _popupNavigation.PushAsync(popup, animated);
                default:
                    return base.DoPush(currentPage, page, useModalNavigation, animated, insertBeforeLast, navigationOffset);
            }
        }

        protected override void ApplyPageBehaviors(Page page)
        {
            switch(page)
            {
                case PopupPage popup:
                    page.Behaviors.Add(new BackgroundPopupDismissalBehavior(_popupNavigation, _applicationProvider));
                    break;
                default:
                    base.ApplyPageBehaviors(page);
                    break;
            }
        }

        protected override Page GetCurrentPage()
        {
            if(_popupNavigation.PopupStack.Any())
            {
                return _popupNavigation.PopupStack.LastOrDefault();
            }

            return base.GetCurrentPage();
        }
    }
}
