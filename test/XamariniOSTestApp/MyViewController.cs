using System;

using UIKit;
using Auth0.OidcClient;
using IdentityModel.OidcClient;
using Foundation;
using System.Text;
using CoreGraphics;

namespace XamariniOSTestApp
{
	public partial class MyViewController : UIViewController
	{
		private Auth0Client _client;

		public MyViewController() : base("MyViewController", null)
		{

		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			UserDetailsTextView.Text = String.Empty;

			View.BackgroundColor = UIColor.Green;
			LoginButton.TouchUpInside += LoginButton_TouchUpInside;			
		}


		private async void LoginButton_TouchUpInside(object sender, EventArgs e)
		{
			_client = new Auth0Client(new Auth0ClientOptions
			{
				Domain = "burnable.auth0.com",
				ClientId = "ID7tyY5U7toxI5cClvjZ3B0N0r4JpsNk",
				Controller = this,
				UseWKWebView = true				
		    });

			_client.SetAutoClose(true);
			_client.SetOnCancel(wv =>
			{
				var bla = 0;
				bla++;
			});

			_client.SetOnSuccess(wv =>
			{
				var bla = 0;
				bla++;
			});

			_client.SetWKWebViewFrame(new CGRect(50, 50, View.Frame.Width - 100, View.Frame.Height - 100));
			_client.SetOnHideCallbackHandler("callbackHandler");
			_client.SetOnHideMessageName("hide");
			_client.DisableZooming(true);
			_client.DisableBouncing(true);
			_client.DisableScrolling(true);

			var loginResult = await _client.LoginAsync();

            var sb = new StringBuilder();

            if (loginResult.IsError)
            {
                sb.AppendLine("An error occurred during login:");
                sb.AppendLine(loginResult.Error);
            }
            else
            {
                sb.AppendLine($"ID Token: {!string.IsNullOrEmpty(loginResult.IdentityToken)}");
                sb.AppendLine($"Access Token: {!string.IsNullOrEmpty(loginResult.AccessToken)}");
                sb.AppendLine($"Refresh Token: {!string.IsNullOrEmpty(loginResult.RefreshToken)}");
                sb.AppendLine();
                sb.AppendLine("-- Claims --");
                foreach (var claim in loginResult.User.Claims)
                {
                    sb.AppendLine($"{claim.Type} = {claim.Value}");
                }
            }

            UserDetailsTextView.Text = sb.ToString();
		}
	}
}

