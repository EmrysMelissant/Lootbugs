using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AI;
public class AI : MonoBehaviour
{
    NavMeshAgent agent;
    Animator animator;
    State currentState;
    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
        animator = this.GetComponent<Animator>();
        currentState = new Idle(this.gameObject, agent, animator, null);
    }

    
    void Update()
    {
        currentState = currentState.Process();
    }
}
