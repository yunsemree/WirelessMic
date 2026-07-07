using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace WirelessMic.App.Platforms.Android.Services;

/// <summary>
/// Mikrofon kaydı sırasında arka planda çalışan foreground servis.
/// </summary>
[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeMicrophone)]
public sealed class MicrophoneForegroundService : Service
{
    private const int NotificationId = 1001;
    private const string ChannelId = "wirelessmic_microphone";

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();
        var notification = BuildNotification();
        StartForeground(NotificationId, notification);
        return StartCommandResult.Sticky;
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var channel = new NotificationChannel(
            ChannelId,
            "WirelessMic Mikrofon",
            NotificationImportance.Low);

        var manager = GetSystemService(NotificationService) as NotificationManager;
        manager?.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification()
    {
        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("WirelessMic")
            .SetContentText("Mikrofon aktif")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetOngoing(true)
            .Build();
    }
}
