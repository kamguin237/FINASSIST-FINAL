namespace FinAssist.Core.Entities;

public enum StatutBesoin
{
    BROUILLON,
    ENREGISTRE,
    SOUMISE,
    EN_ATTENTE,     // Soumis par l'agent, en attente de prise en charge
    APPROUVEE1,     // Approuvé par le Responsable (N1)
    SIGNEE1,        // Signé par le Responsable (N1)
    TRANSMIS,       // Transmis à la Direction (N2)
    APPROUVEE2,     // Approuvé par la Direction (N2)
    SIGNEE2,        // Signé par la Direction (N2)
    REJETEE_N1,
    REJETEE_N2
}
