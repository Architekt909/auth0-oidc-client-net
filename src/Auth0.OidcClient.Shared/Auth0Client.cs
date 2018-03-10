using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IdentityModel.OidcClient.Browser;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Results;

#if __IOS__
using SafariServices;
using WebKit;
using CoreGraphics;
#endif

namespace Auth0.OidcClient
{
    public class Auth0Client
    {
        private readonly Auth0ClientOptions _options;
        private readonly IdentityModel.OidcClient.OidcClient _oidcClient;

        /// <summary>
        /// Creates a new instance of the Auth0 OIDC Client.
        /// </summary>
        /// <param name="options">The <see cref="Auth0ClientOptions"/> specifying the configuration for the Auth0 OIDC Client.</param>
        public Auth0Client(Auth0ClientOptions options)
        {
            _options = options;

            var authority = $"https://{options.Domain}";
#if __ANDROID__
            string packageName = options.Activity.Application.ApplicationInfo.PackageName;
#endif

#if __IOS__
			var redirectUri = $"{Foundation.NSBundle.MainBundle.BundleIdentifier}://{options.Domain}/ios/{Foundation.NSBundle.MainBundle.BundleIdentifier}/callback";
			IBrowser browser;
			if (options.UseWKWebView)
				browser = new PlatformWKWebView(options.Controller, redirectUri);
			else
				browser = new PlatformWebView(options.Controller);
#endif

			var oidcClientOptions = new OidcClientOptions
            {
                Authority = authority,
                ClientId = options.ClientId,
                ClientSecret = options.ClientSecret,
                Scope = options.Scope,
                LoadProfile = options.LoadProfile,
#if __IOS__
				RedirectUri = redirectUri,
				Browser = browser,
#elif __ANDROID__
				RedirectUri = options.RedirectUri ?? $"{packageName}://{options.Domain}/android/{packageName}/callback".ToLower(),
                Browser = new PlatformWebView(options.Activity),
#elif WINDOWS_UWP
				RedirectUri = Windows.Security.Authentication.Web.WebAuthenticationBroker.GetCurrentApplicationCallbackUri().AbsoluteUri,
                Browser = options.Browser ?? new PlatformWebView(),
#else
                RedirectUri = options.RedirectUri ?? $"https://{options.Domain}/mobile",
                Browser = options.Browser ?? new PlatformWebView(),
#endif
                Flow = OidcClientOptions.AuthenticationFlow.AuthorizationCode,
#if WINDOWS_UWP
                ResponseMode = OidcClientOptions.AuthorizeResponseMode.FormPost,
#else
                ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect,
#endif
                Policy =
                {
                    RequireAuthorizationCodeHash = false,
                    RequireAccessTokenHash = false
                }
            };
            _oidcClient = new IdentityModel.OidcClient.OidcClient(oidcClientOptions);			
        }

#if __IOS__
		// Only used when UseWKWebView is false, ignored otherwise
		public void ShowAnimatedForSafariView(bool animated)
		{
			var view = (PlatformWebView)_oidcClient.Options.Browser;
			view.ShowAnimated = animated;
		}

		/*
			Only used when UseWKWebView is false, ignored otherwise. Example of what you can do:

			// This will "hide" the top and bottom bars by simply extending the view height. Looks kind of ghetto on an iPhone X
			safari =>
			{
				var frame = safari.View.Frame;

				// not doing this cause on the X it looks super ghetto
				//var topBarHeight = 64;	// removes top bar entirely

				var topBarHeight = 0;
				var heightPad = 70;     // adds enough padding so that the bottom toolbar gets pushed offscreen. Account for X as well as non-X phones.

				frame.Y = frame.Y - topBarHeight;
				frame.Size = new CGSize(frame.Width, frame.Height + topBarHeight + heightPad);
				safari.View.Frame = frame;
			});

		 */
		public void SetOnSafariDisplayedHandler(Action<SFSafariViewController> handler)
		{
			var view = (PlatformWebView)_oidcClient.Options.Browser;
			view.OnSafariDisplayed = handler;
		}

		private PlatformWKWebView PlatformWKWebView => (PlatformWKWebView)_oidcClient.Options.Browser;

		// Only used when UseWKWebView is true: will automatically close the view after successfully logging in or canceling
		public void SetAutoClose(bool close) => PlatformWKWebView.AutoClose = close;		

		// Only used when UseWKWebView is true: Sets a handler to be called in the event of user cancelation. Optional.
		public void SetOnCancel(Action<WKWebView> onCancel) => PlatformWKWebView.OnCancel = onCancel;		

		// Only used when UseWKWebView is true: Sets a handler to be called in the event of user cancelation. Optional.
		public void SetOnSuccess(Action<WKWebView> onSuccess) => PlatformWKWebView.OnSuccess = onSuccess;

		// If true, prevents the web view from bouncing if you scroll and release
		public void DisableBouncing(bool disable) => PlatformWKWebView.DisableBouncing = disable;

		// If true, prevents the web view from being able to be zoomed in. This will also prevent zooming when the user types in a text field
		public void DisableZooming(bool disable) => PlatformWKWebView.DisableZooming = disable;

		// If true, prevents the web view from being able to scroll at all
		public void DisableScrolling(bool disable) => PlatformWKWebView.DisableScrolling = disable;

		// Sets a custom frame for the login window (only if UseWKWebView is true). If not set, uses the controller's view frame.
		public void SetWKWebViewFrame(CGRect frame) => PlatformWKWebView.WKWebViewFrame = frame;

		/*
			If you want to be able to catch the user clicking the "X" aka close button on your login screen, so you can detect cancelation,
			you need to do a couple things.
			1. Edit your login page javascript to set the lock widget to allow closing. 
				If you're using a hosted page just click "Hosted Pages" from the dashboard, select
				"Customize Login Page", then edit the document. In the lock instantiation, add "closable: true". If you have a default HTML
				implementation, you could do this after the "auth: {...}" setting, or wherever you want.
			2. Add a message to be posted when the lock close button is pressed. If you're using a hosted page with the default HTML implementation, do this after
				the instantiation of lock. I.e. after the large "var lock = new Auth0Lock(....":
		
				lock.on('hide', () => 
				{      
					webkit.messageHandlers.callbackHandler.postMessage("hide");
				});

			There's 2 important things to note here. The first is "callbackHandler". You can call this whatever you want. 
			The second is the string "hide" in the postMessage(...) call. You can make this whatever you want as well.
			Note: don't change the word	'hide' specified as the lock.on(...) first parameter: Auth0 lock will emit this when the X button is pressed.

			If you don't set these two properties, and/or you don't edit your hosted lock page, then we won't be able respond to the user closing the window.
		 */
		public void SetOnHideCallbackHandler(string callbackHandler) => PlatformWKWebView.JavascriptCallbackHandlerName = callbackHandler;
		public void SetOnHideMessageName(string messageName) => PlatformWKWebView.JavascriptOnLockHideEventName = messageName;
#endif

		private Dictionary<string, string> AppendTelemetry(object values)
        {
            var dictionary = ObjectToDictionary(values);

            if (_options.EnableTelemetry)
                dictionary.Add("auth0Client", CreateTelemetry());

            return dictionary;
        }

        private string CreateTelemetry()
        {
#if __ANDROID__
            string platform = "xamarin-android";
#elif __IOS__
            string platform = "xamarin-ios";
#elif WINFORMS
            string platform = "winforms";
#elif WPF
            string platform = "wpf";
#elif WINDOWS_UWP
            var platform = "uwp";
#endif
            var version = GetType().GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()
                .Version;

            var telemetryString = $"{{\"name\":\"oidc-net\",\"version\":\"{version}\",\"platform\":\"{platform}\"}}";
            var telemetryBytes = Encoding.UTF8.GetBytes(telemetryString);

            return Convert.ToBase64String(telemetryBytes);
        }

        /// <summary>
        /// Launches a browser to log the user in.
        /// </summary>
        /// <param name="extraParameters">Any extra parameters that need to be passed to the authorization endpoint.</param>
        /// <returns></returns>
        public Task<LoginResult> LoginAsync(object extraParameters = null)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return _oidcClient.LoginAsync(extraParameters: AppendTelemetry(extraParameters));
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private Dictionary<string, string> ObjectToDictionary(object values)
        {
            var dictionary = values as Dictionary<string, string>;
            if (dictionary != null)
                return dictionary;

            dictionary = new Dictionary<string, string>();
            if (values != null)
                foreach (var prop in values.GetType().GetRuntimeProperties())
                {
                    var value = prop.GetValue(values) as string;
                    if (!string.IsNullOrEmpty(value))
                        dictionary.Add(prop.Name, value);
                }

            return dictionary;
        }

        /// <summary>
        /// Generates an <see cref="AuthorizeState"/> containing the URL, state, nonce and code challenge which can
        /// be used to redirect the user to the authorization URL, and subsequently process any response by calling
        /// the <see cref="ProcessResponseAsync"/> method.
        /// </summary>
        /// <param name="extraParameters"></param>
        /// <returns></returns>
        public Task<AuthorizeState> PrepareLoginAsync(object extraParameters = null)
        {
            return _oidcClient.PrepareLoginAsync(AppendTelemetry(extraParameters));
        }

        /// <summary>
        /// Process the response from the Auth0 redirect URI
        /// </summary>
        /// <param name="data">The data containing the full redirect URI.</param>
        /// <param name="state">The <see cref="AuthorizeState"/> which was generated when the <see cref="PrepareLoginAsync"/>
        /// method was called.</param>
        /// <returns></returns>
        public Task<LoginResult> ProcessResponseAsync(string data, AuthorizeState state)
        {
            return _oidcClient.ProcessResponseAsync(data, state);
        }

        /// <summary>
        /// Generates a new set of tokens based on a refresh token. 
        /// </summary>
        /// <param name="refreshToken">The refresh token which was issued during the authorization flow, or subsequent
        /// calls to <see cref="RefreshTokenAsync"/>.</param>
        /// <returns></returns>
        public Task<RefreshTokenResult> RefreshTokenAsync(string refreshToken)
        {
            return _oidcClient.RefreshTokenAsync(refreshToken);
        }
    }
}