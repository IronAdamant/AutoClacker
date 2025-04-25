using System;
using System.Windows;
using AutoClacker.Views;

namespace AutoClacker
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            var app = new Application();
            app.Run(new MainWindow());
        }
    }
}