namespace FiveNightAnomaly.Combat
{
    /// <summary>Anything an attack can hit. Put the component on the object with the Collider2D or on one of its parents.</summary>
    public interface IHittable
    {
        void TakeHit(in HitInfo hit);
    }
}
