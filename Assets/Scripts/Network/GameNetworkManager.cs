using Unity.Netcode;
using UnityEngine;

public class GameNetworkManager : NetworkManager
{
    NetworkVariable<int> playerScore = new NetworkVariable<int>(0);
}
