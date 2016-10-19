using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerManager {
    public static PlayerManager Manager {
        get { return s_manager; }
    }
    public Player Me {
        get {
            return m_players.Find(x => x.IsMe);
        }
    }

    private static PlayerManager s_manager = new PlayerManager();

    private List<Player> m_players = new List<Player>();

    public void RegisterPlayer(Player newPlayer) {
        m_players.Add(newPlayer);
    }
}
