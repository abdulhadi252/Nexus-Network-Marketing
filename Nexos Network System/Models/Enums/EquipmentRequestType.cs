namespace Nexos_Network_System.Models.Enums;

/// <summary>
/// What the customer wants technical to do with equipment that is currently assigned to them.
/// </summary>
public enum EquipmentRequestType
{
    /// <summary>The customer wants the assignment cancelled.</summary>
    Cancellation,

    /// <summary>The customer wants to hand the equipment back.</summary>
    Return
}
