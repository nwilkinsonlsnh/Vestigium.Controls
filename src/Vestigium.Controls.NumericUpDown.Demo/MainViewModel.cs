using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Controls.NumericUpDown;

namespace Vestigium.Controls.NumericUpDown.Demo;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private decimal _liveValue;
    [ObservableProperty] private decimal _deferredValue = 8;
    [ObservableProperty] private decimal _immediateValue = 8;
    [ObservableProperty] private decimal _snapValue = 7;
    [ObservableProperty] private decimal _ttl = 64;
    [ObservableProperty] private decimal _money = 12.5m;
    [ObservableProperty] private decimal _spinOnly = 3;
    [ObservableProperty] private decimal _readOnly = 42;
    [ObservableProperty] private decimal _nestedHops = 4;
    [ObservableProperty] private decimal _nestedTimeout = 1000;
    [ObservableProperty] private decimal _labValue;
    [ObservableProperty] private int _liveCommits;
    [ObservableProperty] private VestigiumNumericUpdateMode _labUpdateMode = VestigiumNumericUpdateMode.Immediate;
    [ObservableProperty] private VestigiumNumericCommitMode _labCommitMode = VestigiumNumericCommitMode.Auto;
    [ObservableProperty] private VestigiumNumericInputMode _labInputMode = VestigiumNumericInputMode.Full;
    [ObservableProperty] private decimal _increment = 1;
    [ObservableProperty] private decimal _pageIncrement = 10;
    [ObservableProperty] private int _accelerationDelay = 2000;
    [ObservableProperty] private int _decimalPlaces;
    [ObservableProperty] private bool _snapEnabled;
    [ObservableProperty] private VestigiumNumericSnapMode _snapMode = VestigiumNumericSnapMode.Round;

    partial void OnLiveValueChanged(decimal value) => LiveCommits++;

    [RelayCommand] private void SetImmediate() => LabUpdateMode = VestigiumNumericUpdateMode.Immediate;
    [RelayCommand] private void SetDeferred() => LabUpdateMode = VestigiumNumericUpdateMode.Deferred;
    [RelayCommand] private void SetAuto() => LabCommitMode = VestigiumNumericCommitMode.Auto;
    [RelayCommand] private void SetExplicit() => LabCommitMode = VestigiumNumericCommitMode.Explicit;
    [RelayCommand] private void SetFull() => LabInputMode = VestigiumNumericInputMode.Full;
    [RelayCommand] private void SetSpinOnly() => LabInputMode = VestigiumNumericInputMode.SpinOnly;
    [RelayCommand] private void SetReadOnly() => LabInputMode = VestigiumNumericInputMode.ReadOnly;
}
