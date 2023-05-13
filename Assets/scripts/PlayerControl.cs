using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    public GameControl gameManager;

    // 在 Awake 方法中获取 GameManager 单例对象
    private void Awake()
    {
        
    }

    // 在 Update 方法中检测玩家输入并调用 GameManager 中对应的移动函数
    private void Update()
    {
        checkMove();
        
    }

    private void checkMove()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            gameManager.MoveToLeft();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            gameManager.MoveToRight();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            gameManager.MoveToUp();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            gameManager.MoveToDown();
        }

        // 更新玩家位置
        this.transform.position = new Vector3(gameManager.playerPosX, gameManager.playerPosY,-3);
    }
}
