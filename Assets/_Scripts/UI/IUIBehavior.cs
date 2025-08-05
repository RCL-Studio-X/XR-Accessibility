using UnityEngine;

public interface IUIBehavior
{
    void OnUIShown();
    void OnUIHidden();
    void UpdateBehavior();
    bool IsActive { get; }
}