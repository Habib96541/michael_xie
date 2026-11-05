namespace CyberWhack.Data
{
    public enum TargetType
    {
        None = 0,
        StandardCyberMole = 1,   // Active 0.85s, 1 tap, +100 base score
        OverclockedGoldMole = 2, // Active 0.50s, 1 tap, +300 base score
        ArmoredTankMole = 3,     // Active 1.10s, 2 taps, +250 base score (first cracks visor)
        EmpDecoyMine = 4         // Active 1.00s, 1 tap, -200 score penalty & combo reset
    }
}
