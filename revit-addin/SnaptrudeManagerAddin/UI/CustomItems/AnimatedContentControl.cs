using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SnaptrudeManagerAddin.CustomItems
{
    public class AnimatedContentControl : ContentControl
    {
        public AnimatedContentControl()
        {
            DefaultStyleKey = typeof(AnimatedContentControl);
            this.Loaded += (s, e) => PlayAnimation();
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            PlayAnimation();
        }

        private void PlayAnimation()
        {
            Storyboard storyboard = this.FindResource("FadeInAnimation") as Storyboard;
            if (storyboard != null)
                storyboard.Begin();
        }
    }
}