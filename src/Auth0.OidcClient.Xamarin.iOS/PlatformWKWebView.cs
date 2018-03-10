using System;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using IdentityModel.OidcClient.Browser;
using UIKit;
using WebKit;

namespace Auth0.OidcClient
{
	// This is my custom version that uses a WKWebView instead of a SFSafariViewController so we can customize the shit out of it
	class PlatformWKWebView : WKNavigationDelegate, IWKUIDelegate, IBrowser, IWKScriptMessageHandler
	{
		private WKWebView _webView;
		private readonly UIViewController _controller;
		private readonly string _redirectUri;

		private class DisableZoomDelegate : UIScrollViewDelegate
		{
			public override UIView ViewForZoomingInScrollView(UIScrollView scrollView)
			{
				return null;
			}
		}

		// See Auth0Client for a detailed explanation of these two parameters
		public string JavascriptCallbackHandlerName { get; set; }
		public string JavascriptOnLockHideEventName { get; set; }

		// If not specified, uses the controller's entire view frame
		public CGRect WKWebViewFrame { get; set; } = CGRect.Null;

		// If true, prevents the web view from bouncing if you scroll and release
		public bool DisableBouncing { get; set; }

		// If true, prevents the web view from being able to be zoomed in. This will also prevent zooming when the user types in a text field
		public bool DisableZooming { get; set; }

		// If true, prevents the web view from being able to scroll at all
		public bool DisableScrolling { get; set; }

		// If true, will automatically close the login window upon successfully logging in. 
		public bool AutoClose { get; set; }

		// A handler to be called (optionally) if the user cancels the login. Passes in the web view.
		public Action<WKWebView> OnCancel { get; set; }

		// A handler to be called (optionally) upon successfully logging in. This is called right before auto closing of the view.
		// If auto close is false, this is still called and could be used to customize the closing of the view. Passes in the web view.
		public Action<WKWebView> OnSuccess { get; set; }

		public PlatformWKWebView(UIViewController controller, string redirectUri)
		{
			_controller = controller;
			_redirectUri = redirectUri.ToLower();
		}

		// Let's us respond to javascript postMessage calls. We'll use this to listen for the close button being pressed.
		public void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
		{
			var content = message.Body.ToString();
			if (content.Equals(JavascriptOnLockHideEventName, StringComparison.OrdinalIgnoreCase))
				ActivityMediator.Instance.Send("UserCancel");
		}


		// Takes the place of overriding AppDelegate.OpenUrl
		public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
		{			
			var url = navigationAction.Request.Url.ToString().ToLower();
			if (url.StartsWith(_redirectUri, StringComparison.Ordinal))
				ActivityMediator.Instance.Send(navigationAction.Request.Url.AbsoluteString);
			else
				decisionHandler(WKNavigationActionPolicy.Allow);
		}		

		public Task<BrowserResult> InvokeAsync(BrowserOptions options)
		{
			if (string.IsNullOrWhiteSpace(options.StartUrl))
			{
				throw new ArgumentException("Missing StartUrl", nameof(options));
			}

			if (string.IsNullOrWhiteSpace(options.EndUrl))
			{
				throw new ArgumentException("Missing EndUrl", nameof(options));
			}

			// must be able to wait for the intent to be finished to continue
			// with setting the task result
			var tcs = new TaskCompletionSource<BrowserResult>();			

			var config = new WKWebViewConfiguration
			{
				Preferences = new WKPreferences { JavaScriptEnabled = true }
			};

			// Listen for the user clicking the close button
			if (!string.IsNullOrEmpty(JavascriptOnLockHideEventName) && !string.IsNullOrEmpty(JavascriptCallbackHandlerName))
			{
				var cc = new WKUserContentController();
				cc.AddScriptMessageHandler(this, JavascriptCallbackHandlerName);
				config.UserContentController = cc;
			}

			// Make our browser view
			_webView = new WKWebView(WKWebViewFrame == CGRect.Null ? _controller.View.Frame : WKWebViewFrame, config);
			_webView.UIDelegate = this;
			_webView.NavigationDelegate = this;

			if (DisableBouncing)
				_webView.ScrollView.Bounces = false;

			if (DisableScrolling)
				_webView.ScrollView.ScrollEnabled = false;

			if (DisableZooming)
				_webView.ScrollView.Delegate = new DisableZoomDelegate();


			void Callback(string response)
			{
				// remove handler
				ActivityMediator.Instance.ActivityMessageReceived -= Callback;

				if (DisableZooming)
					_webView.ScrollView.Delegate = null;

				if (response == "UserCancel")
				{					
					tcs.SetResult(new BrowserResult {ResultType = BrowserResultType.UserCancel});

					OnCancel?.Invoke(_webView);

					if (AutoClose)
					{
						_webView.RemoveFromSuperview();
						UIApplication.SharedApplication.KeyWindow.SetNeedsLayout();
					}
				}
				else
				{
					// set result
					tcs.SetResult(new BrowserResult
					{
						Response = response,
						ResultType = BrowserResultType.Success
					});

					// Close web view
					OnSuccess?.Invoke(_webView);

					if (AutoClose)
					{
						_webView.RemoveFromSuperview();
						UIApplication.SharedApplication.KeyWindow.SetNeedsLayout();
					}					
				}
			}

			// attach handler
			ActivityMediator.Instance.ActivityMessageReceived += Callback;

			// launch browser
			_controller.Add(_webView);
			_webView.LoadRequest(new NSUrlRequest(new NSUrl(options.StartUrl)));

			// need an intent to be triggered when browsing to the "io.identitymodel.native://callback"
			// scheme/URI => CallbackInterceptorActivity
			return tcs.Task;
		}

	}
}