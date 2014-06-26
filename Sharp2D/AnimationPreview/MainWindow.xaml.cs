﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Sharp2D.Core.Graphics;
using Sharp2D.Game.Sprites;
using Sharp2D.Game.Sprites.Animations;
using AnimationPreview.Preview;
using Sharp2D.Game.Worlds;
using System.IO;
using System.Security.Permissions;
using System.Threading;
using System.Runtime.InteropServices;
using System.Drawing;
using Ookii.Dialogs.Wpf;

namespace AnimationPreview
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        string json;
        string image;
        TempSprite sprite;
        EmptyWorld world;
        FileSystemWatcher watcher;
        Thread updater;
        bool run = true;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SpriteRenderJob.SetDefaultJob<OpenGL3SpriteRenderJob>();

            Screen.DisplayScreenAsync();

            updater = new Thread(new ThreadStart(delegate
            {
                string otext = "";
                while (run)
                {
                    try
                    {
                        Dispatcher.Invoke(new Action(delegate
                        {
                            if (otext != code.Text)
                            {
                                Button_Click(null, null);

                                otext = code.Text;
                            }
                        }));

                        Thread.Sleep(1000);
                    }
                    catch
                    {

                    }
                }
            }));
            //updater.Start();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            run = false;
            updater.Interrupt();

            Screen.TerminateScreen();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(json) && !string.IsNullOrWhiteSpace(code.Text))
            {
                File.WriteAllText(json, code.Text);
                if (sprite != null)
                {
                    sprite.ClearAnimations();
                }
            }

            json = json_path.Text;
            image = sheet_path.Text;

            if (sprite == null)
            {
                sprite = new TempSprite();
            }
            if (world == null)
            {
                world = new EmptyWorld();
                world.Load();
                world.Display();
            }
            if (watcher == null)
            {
                WatchImageFile(image);
            }

            sprite.TexPath = image;
            sprite.JsonPath = json;
            
            if (world.Sprites.Contains(sprite))
            {
                world.RemoveSprite(sprite);
            }
            world.AddSprite(sprite);

            code.Text = File.ReadAllText(json);

            //sprite.Y = 600;

            sheet_preview.Source = ToBitmapSource(sprite.Texture.Bitmap);

            Screen.Camera.Z = 200;

            animations.Items.Clear();

            for (int row = 0; row < sprite.Animations.Rows; row++)
            {
                animations.Items.Add(sprite.Animations[row].Name);
            }

            //Screen.Camera.Y = 630f;
        }

        void box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (animations.SelectedIndex != -1)
            {
                sprite.Animations[(string)animations.SelectedItem].Play();
            }
        }

        [PermissionSet(SecurityAction.Demand, Name="FullTrust")]
        private void WatchImageFile(string path)
        {
            watcher = new FileSystemWatcher();
            watcher.Path = new FileInfo(image).Directory.FullName;

            watcher.Changed += watcher_Changed;

            watcher.NotifyFilter = NotifyFilters.LastWrite;

            watcher.EnableRaisingEvents = true;
        }

        void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.Name == image)
            {
                sprite.TexPath = image;
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    sheet_preview.Source = ToBitmapSource(sprite.Texture.Bitmap);
                }));
            }
        }

        public static ImageSource ToBitmapSource(System.Drawing.Bitmap source)
        {
            IntPtr hBitmap = source.GetHbitmap();
            ImageSource isource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            //source.Dispose();
            DeleteObject(hBitmap);

            return isource;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabEditor.IsSelected && !selected)
            {
                selected = true;
                if (original_texture == null)
                    original_texture = ToBitmapSource(sprite.Texture.Bitmap);
                
                sheet_editor.Source = original_texture;
                for (int row = 0; row < sprite.Animations.Rows; row++)
                {
                    editor_animations.Items.Add(sprite.Animations[row].Name);
                }
            }
            else if (!TabEditor.IsSelected)
            {
                selected = false;
                //TODO Unload more shit
                sheet_editor.Source = null;
                editor_animations.Items.Clear();
                Frames.Children.Clear();
            }
        }

        private Animation currently_editing_animation;
        private ImageSource original_texture;
        private int currently_editing_animation_row;
        private bool selected = false;
        private int selectedIndex = -1;
        private void editor_animations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!TabEditor.IsSelected)
                return;

            currently_editing_animation = sprite.Animations[(string)editor_animations.SelectedItem];
            currently_editing_animation_row = editor_animations.SelectedIndex;

            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(sprite.Texture.Bitmap); //Clone it, we don't want to write on our texture

            System.Drawing.Pen bluePen = new System.Drawing.Pen(System.Drawing.Color.Aqua, 2);

            int x = 0;
            int y = 0;
            for (int i = 0; i < currently_editing_animation.Row; i++)
            {
                y += currently_editing_animation.Owner.Animations[i].Height;
            }

            float width = currently_editing_animation.Width * currently_editing_animation.Frames;
            float height = y + currently_editing_animation.Height;

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawLine(bluePen, x, y, width, y);
                graphics.DrawLine(bluePen, width, y, width, height);
                graphics.DrawLine(bluePen, x, height, width, height);
                graphics.DrawLine(bluePen, x, y, x, height);
            }

            ImageSource @new = ToBitmapSource(bitmap);

            bitmap.Dispose();

            sheet_editor.Source = @new;

            //Create frames

            Frames.Children.Clear();
            bitmap = new System.Drawing.Bitmap(sprite.Texture.Bitmap); //Clone it, we don't want to write on our texture
            for (int i = 0; i < currently_editing_animation.Frames; i++)
            {
                Bitmap result = new Bitmap(currently_editing_animation.Width, currently_editing_animation.Height);

                x = currently_editing_animation.Width * i;
                y = 0;
                for (int z = 0; z < currently_editing_animation.Row; z++)
                {
                    y += currently_editing_animation.Owner.Animations[i].Height;
                }

                width = currently_editing_animation.Width;
                height = currently_editing_animation.Height;

                using (var g = Graphics.FromImage(result))
                {
                    g.DrawImage(bitmap, new RectangleF(0f, 0f, width, height), new RectangleF(x, y, width, height), GraphicsUnit.Pixel);
                }

                Button btn = new Button();
                btn.Name = "b" + i;
                btn.Width = 32;
                btn.ContextMenu = FindResource("cmButton") as ContextMenu;
                btn.ContextMenu.PlacementTarget = btn;

                btn.Click += delegate
                {
                    try
                    {
                        selectedIndex = int.Parse(btn.Name.Substring(1));
                        btn.ContextMenu.IsOpen = !btn.ContextMenu.IsOpen;
                    }
                    catch { }
                };

                System.Windows.Controls.Image img = new System.Windows.Controls.Image();
                img.Source = ToBitmapSource(result);
                btn.Content = img;

                result.Dispose();

                Frames.Children.Add(btn);
            }
            /*Button btn1 = new Button();
            btn1.Name = "add";
            btn1.Width = 82;

            Grid grid = new Grid();
            grid.Height = 66;

            System.Windows.Controls.Image image = new System.Windows.Controls.Image();
            try
            {
                image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/plus.png"));
            }
            catch { }
            image.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            image.Width = 32;

            Label label = new Label();
            label.Content = "Add Frame";
            label.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;

            grid.Children.Add(image);
            grid.Children.Add(label);

            btn1.Content = grid;

            Frames.Children.Add(btn1);*/

            bitmap.Dispose();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private int target;
        private ProgressDialog progress;
        private Bitmap newImage;
        private void cm_before_Click(object sender, RoutedEventArgs e)
        {
            target = selectedIndex - 1;
            AddFrame();
        }

        private void cm_after_Click(object sender, RoutedEventArgs e)
        {
            target = selectedIndex + 1;
            AddFrame();
        }

        private void cm_remove_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void AddFrame()
        {
            VistaOpenFileDialog dialog = new VistaOpenFileDialog();
            dialog.Filter = "Image Files(*.bmp;*.jpg;*.gif;*.png)|*.bmp;*.jpg;*.gif;*.png";

            if ((bool)dialog.ShowDialog(this))
            {
                newImage = new Bitmap(dialog.FileName, false);
                progress = new ProgressDialog()
                {
                    WindowTitle = "Adding frame",
                    Text = "Please wait while the frame is added...",
                    ShowTimeRemaining = true,
                    ShowCancelButton = true
                };
                progress.ProgressBarStyle = ProgressBarStyle.MarqueeProgressBar;
                progress.DoWork += progress_DoWork;
                progress.Show();
            }
        }

        void progress_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (target == -1) //Add to start
            {
                float x = 0;
                float y = 0;
                for (int i = 0; i < currently_editing_animation.Row; i++)
                {
                    y += currently_editing_animation.Owner.Animations[i].Height;
                }

                float width = currently_editing_animation.Width * currently_editing_animation.Frames;
                float height = y + currently_editing_animation.Height;

                Bitmap result = new Bitmap((int)width + currently_editing_animation.Width, (int)height);
                int totalWidth = Math.Max(sprite.Texture.Bitmap.Width, result.Width);
                Bitmap final = new Bitmap(totalWidth, sprite.Texture.Bitmap.Height);

                using (var g = Graphics.FromImage(result))
                {
                    g.DrawImage(sprite.Texture.Bitmap, new RectangleF(currently_editing_animation.Width, y, width, currently_editing_animation.Height), new RectangleF(x, y, width, height), GraphicsUnit.Pixel);
                    progress.ReportProgress(25, null, "Moving frames over");
                    g.DrawImage(newImage, new RectangleF(0, y, currently_editing_animation.Width, currently_editing_animation.Height), new RectangleF(0, 0, currently_editing_animation.Width, currently_editing_animation.Height), GraphicsUnit.Pixel);
                    progress.ReportProgress(50, null, "Placing new frame");
                }

                using (var finalG = Graphics.FromImage(final))
                {
                    finalG.DrawImage(sprite.Texture.Bitmap, new RectangleF(0, 0, sprite.Texture.Bitmap.Width, sprite.Texture.Bitmap.Height), new RectangleF(0, 0, sprite.Texture.Bitmap.Width, sprite.Texture.Bitmap.Height), GraphicsUnit.Pixel);
                    finalG.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    using (var br = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 255, 255, 255)))
                    {
                        finalG.FillRectangle(br, x, y, width + currently_editing_animation.Width, height);
                    }
                    finalG.DrawImage(result, new RectangleF(x, y, width + currently_editing_animation.Width, height), new RectangleF(0, 0, result.Width, result.Height), GraphicsUnit.Pixel);
                }

                progress.ReportProgress(75, null, "Saving image..");
                final.Save(this.image);

                progress.ReportProgress(100, null, "Reloading..");
                Dispatcher.Invoke(new Action(delegate
                {
                    Button_Click(null, null);
                    Preview.IsSelected = true;
                    Thread.Sleep(1500);
                    TabEditor.IsSelected = true;
                }));
                result.Dispose();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog dialog = new VistaOpenFileDialog();
            dialog.Filter = "Config Files(*.conf;*.json)|*.conf;*.json";

            if ((bool)dialog.ShowDialog(this))
            {
                json_path.Text = dialog.FileName;
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog dialog = new VistaOpenFileDialog();
            dialog.Filter = "Image Files(*.bmp;*.jpg;*.gif;*.png)|*.bmp;*.jpg;*.gif;*.png";

            if ((bool)dialog.ShowDialog(this))
            {
                sheet_path.Text = dialog.FileName;
            }
        }
    }
}
