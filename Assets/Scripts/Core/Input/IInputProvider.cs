using System;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Move, Fireheld, OnBombPressed, OnPausePressed, OnCrouchedを提供するインターフェース
    /// InputActionの値をこのインターフェースに、読み込む側はこのインターフェースを通して値を取得する
    /// </summary>
    public interface IInputProvider
    {
        Vector2 Move { get; }
        bool FireHeld { get; }
        bool OnCrouched { get; }//しゃがんだタイミングで呼ばれる。
        event Action OnBombPressed;
        event Action OnPausePressed;
    }
}
