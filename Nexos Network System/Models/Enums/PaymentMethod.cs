namespace Nexos_Network_System.Models.Enums;

public enum PaymentMethod
{
    Cash,
    Card,
    BankTransfer,
    Cheque,
    Other,

    /// <summary>JazzCash / EasyPaisa and similar mobile wallets.</summary>
    MobileWallet
}
