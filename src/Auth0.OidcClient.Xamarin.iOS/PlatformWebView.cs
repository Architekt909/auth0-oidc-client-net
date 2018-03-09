using System;
using System.Threading.Tasks;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using IdentityModel.OidcClient.Browser;
using SafariServices;
using UIKit;

namespace Auth0.OidcClient
{
	public class PlatformWebView : SFSafariViewControllerDelegate, IBrowser
	{
		private SafariServices.SFSafariViewController _safari;
		private readonly UIViewController _controller;

		public PlatformWebView(UIViewController controller)
		{
			_controller = controller;
		}

        public override void DidFinish(SFSafariViewController controller)
        {
            ActivityMediator.Instance.Send("UserCancel");
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

			var vcTest = new UIViewController();

			// create Safari controller
			_safari = new SFSafariViewController(new NSUrl(options.StartUrl));
            _safari.Delegate = this;
			_safari.DismissButtonStyle = SFSafariViewControllerDismissButtonStyle.Cancel;

			ActivityMediator.MessageReceivedEventHandler callback = null;
			callback = async (response) =>
			{
				// remove handler
				ActivityMediator.Instance.ActivityMessageReceived -= callback;

                if (response == "UserCancel")
                {
                    tcs.SetResult(new BrowserResult
                    {
                        ResultType = BrowserResultType.UserCancel
                    });
                }
                else
                {
                    // Close Safari
                    await _safari.DismissViewControllerAsync(true);

                    // set result
                    tcs.SetResult(new BrowserResult
                    {
                        Response = response,
                        ResultType = BrowserResultType.Success
                    });
                }
			};

			// attach handler
			ActivityMediator.Instance.ActivityMessageReceived += callback;

			// hmm adding views in _safari above/below or to the _controller didn't let us change the bg color left by the mask.
			// maybe we need to subclass the sfsafari thing? And perhaps add our own colored rectangle on top of the toolbar?
			

			// launch Safari
			_controller.PresentViewController(_safari, true, null);			

			// Jeff: used to hide stupid toolbar
			// So this will create a mask that hides the bottom toolbar. Thing is the mask doesn't care about the color.
			// It only uses the BackgroundColor's alpha. So not setting a color just clips the view. If you set the background color to like
			// maskView.BackgroundColor = UIColor.FromWhiteAlpha(1, 0.5f);
			// you'd have the entire view displaying at half alpha, and then the bottom would still not display cause we clipped it.
			// So in order to have a color behind it, we'd have to have a view below it with whatever color.
			var rect = new CGRect(0, 0, _controller.View.Frame.Width, _controller.View.Frame.Height - 44);
			var maskView = new UIView(rect);
			maskView.BackgroundColor = UIColor.White;
			_safari.View.MaskView = maskView;

			// need an intent to be triggered when browsing to the "io.identitymodel.native://callback"
			// scheme/URI => CallbackInterceptorActivity
			return tcs.Task;
		}
	}
}
