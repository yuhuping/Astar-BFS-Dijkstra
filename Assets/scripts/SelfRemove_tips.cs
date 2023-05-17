using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfRemove_tips : MonoBehaviour
{
    public GameControl gameManager;
    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameControl>();
    }

    // Update is called once per frame
    void Update()
    {

        if(gameManager.playerPosX==this.transform.position.x&& gameManager.playerPosY == this.transform.position.y)
        {
            this.gameObject.SetActive(false);
        }

    }
}
