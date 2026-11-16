using System;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Move, Fireheld, OnBombPressed, OnCrouchedを提供するインターフェース
    /// InputActionの値をこのインターフェースに、読み込む側はこのインターフェースを通して値を取得する
    /// </summary>
    public interface IInputProvider
    {
        Vector2 Move { get; }
        bool Fireheld { get; }
         bool OnCrouched { get; }//しゃがんだタイミングで呼ばれる。
        event Action OnBombPressed;
        Device LastDevice { get; }
    }
}
