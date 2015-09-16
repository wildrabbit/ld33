using UnityEngine;
using System.Collections;

public interface IFSMState<T> where T : new()
{
    int  GetStateID();
    void OnEnter(T reference);
    void OnExit(T reference);
    void OnUpdate(T reference);
}