namespace Vestigium.Controls.StatusBar;

public interface IUpdateStatusBar
{
    void Post(int index, StatusBarUpdate update);
    void Post(string key, StatusBarUpdate update);
    void PostImmediate(int index, StatusBarUpdate update);
    void PostImmediate(string key, StatusBarUpdate update);
    StatusBarSnapshot Snapshot();
}
