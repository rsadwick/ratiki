using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;

using MonoMac;
using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.CoreGraphics;
using System.Runtime.InteropServices;

namespace Platformer
{
	class Program
	{
		static void Main (string [] args)
		{
			NSApplication.Init ();

			using (var p = new NSAutoreleasePool ()) {
				NSApplication.SharedApplication.Delegate = new AppDelegate();
				NSApplication.Main(args);
			}
		}
	}

	class AppDelegate : NSApplicationDelegate
	{
		PlatformerGame game; 

		public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
		{
			game = new PlatformerGame ();
			game.Run();

		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}
	}
}

