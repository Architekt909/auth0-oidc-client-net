using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreGraphics;
using Foundation;
using SafariServices;
using UIKit;

namespace Auth0.OidcClient
{
	class CustomSafari : SFSafariViewController
	{
		public CustomSafari(NSUrl url) : base(url)
		{
		}
		

		public override void ViewDidAppear(bool animated)
		{
			base.ViewDidAppear(animated);

			var view = new UIView(new CGRect(0, 100, View.Frame.Width, 44));
			view.BackgroundColor = UIColor.Green;
			Add(view);

			var bounds = View.Bounds;
			var frame = View.Frame;
			//View.Frame = new CGRect(frame.X, frame.Y, frame.Width, 300);

			
		}
	}
}