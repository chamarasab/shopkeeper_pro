using ShopKeeperPro.Core.Services;

namespace ShopKeeperPro.WinForms.UI;

public class WinFormsSoundPlayer : ISoundPlayer
{
    public void PlaySuccess()
    {
        try
        {
            System.Media.SystemSounds.Beep.Play();
        }
        catch
        {
        }
    }

    public void PlayAlert()
    {
        try
        {
            System.Media.SystemSounds.Hand.Play();
        }
        catch
        {
        }
    }
}
