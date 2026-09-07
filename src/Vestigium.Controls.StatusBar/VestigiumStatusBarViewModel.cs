using CommunityToolkit.Mvvm.ComponentModel;

namespace Vestigium.Controls.StatusBar;

public partial class VestigiumStatusBarViewModel : ObservableObject
{
    public VestigiumStatusBarViewModel()
        : this(new StatusBarEngine())
    {
    }

    public VestigiumStatusBarViewModel(StatusBarEngine engine)
    {
        Engine = engine;
        Engine.Changed += () =>
        {
            OnPropertyChanged(nameof(Position));
            OnPropertyChanged(nameof(Message));
            OnPropertyChanged(nameof(IdleRemainingMs));
        };
    }

    public StatusBarEngine Engine { get; }

    public VestigiumStatusBarPosition Position
    {
        get => Engine.Position;
        set
        {
            if (Engine.Position == value) return;
            Engine.Position = value;
            OnPropertyChanged();
        }
    }

    public string Message
    {
        get => Engine.Columns.FirstOrDefault(c => c.IsLiveRegion)?.Text ?? StatusBarDefaults.ReadyText;
        set => Engine.PostImmediate("message", new StatusBarUpdate { Text = value });
    }

    public int IdleRemainingMs => Engine.Snapshot().IdleRemainingMs;

    public bool IsClockVisible
    {
        get => Engine.Columns.Any(c => c.Kind == StatusBarColumnKind.Clock);
        set { }
    }
}
