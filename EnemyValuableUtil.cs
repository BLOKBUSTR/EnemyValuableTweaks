using Photon.Pun;

#pragma warning disable CS8618
namespace EnemyValuableTweaks
{
    public class EnemyValuableUtil : MonoBehaviourPun
    {
        public EnemyValuable enemyValuable;
        public new PhotonView photonView;
        
        public void SetExplosion(bool state)
        {
            if (SemiFunc.IsNotMasterClient()) return;
            
            photonView.RPC(nameof(SetExplosionRPC), RpcTarget.All, state);
            EnemyValuableTweaks.Debug($"Called SetExplosionRPC on clients (hasExplosion = {state})", enemyValuable);
        }
        
        [PunRPC]
        public void SetExplosionRPC(bool state, PhotonMessageInfo info = default)
        {
            enemyValuable.hasExplosion = state;
            EnemyValuableTweaks.Debug($"Received SetExplosionRPC from host (hasExplosion = {state})", enemyValuable);
        }
    }
}
