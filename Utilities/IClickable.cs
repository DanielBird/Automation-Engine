namespace Engine.Utilities
{
    public interface IClickable
    {
        bool IsEnabled { get; } 
        bool IsSelected { get; }
        void OnPlayerSelect();
        void OnPlayerDeselect();
    }
}