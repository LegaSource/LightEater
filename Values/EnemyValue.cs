namespace LightEater.Values
{
    public class EnemyValue(string enemyName, int absorbDistance, int absorbCharge, bool destroy)
    {
        public string EnemyName { get; internal set; } = enemyName;
        public int AbsorbDistance { get; internal set; } = absorbDistance;
        public int AbsorbCharge { get; internal set; } = absorbCharge;
        public bool Destroy { get; internal set; } = destroy;
    }
}
