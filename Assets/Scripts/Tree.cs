﻿using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public int wins; 
    public int plays; 
    private bool isPlayersTurn;

    private Node parent;
   
    public Dictionary<Node, int> children; //Associate child state with the action that leads to that state.

    public Node(bool _isPlayerTurn = false, Node _parentNode = null)
    {
        wins  = 0;
        plays = 0;
        isPlayersTurn = _isPlayerTurn;
        children = new Dictionary<Node, int>();
        parent = _parentNode;
    }

    private void AddChild(Node _node, int _line)
    {
        children.Add(_node, _line);
    }

    public int GetChildAction(Node _node)
    {
        return children[_node];
    }

    public int GetChildCount()
    {
        return children.Count;
    }

    public Node SelectNodeToExpand(int _nbSimulation, Field _simulatedField)
    {
        //Check for end game.
        if (!_simulatedField.ContainsEmptyCell() || _simulatedField.CheckForVictory())
            return this;

        // Check if not all plays have been tried.
        if (children.Keys.Count != _simulatedField.GetPossibleDrops().Count)
            return this;

        Node bestNode = SelectBestChild(_nbSimulation);
        _simulatedField.DropInColumn(children[bestNode]);
        _simulatedField.SwitchPlayer();

        return bestNode.SelectNodeToExpand(_nbSimulation, _simulatedField);
    }

    // Attach node to tree.
    public Node Expand(Field _simulatedField, System.Random _random)
    {
        if (!_simulatedField.ContainsEmptyCell() || _simulatedField.CheckForVictory())
            return this;

        // Copy of the possible plays list
        List<int> drops = new(_simulatedField.GetPossibleDrops());

        // For each available plays, remove the ones that have already been play.
        foreach (int column in children.Values)
        {
            if (drops.Contains(column))
                drops.Remove(column);
        }

        // Get line to play on.
        int colToPlay = drops[_random.Next(drops.Count)];
        Node node = new Node(_simulatedField.IsPlayersTurn, this);
        AddChild(node, colToPlay); // Adds the child to the tree

        _simulatedField.DropInColumn(colToPlay);
        _simulatedField.SwitchPlayer();

        return node;
    }

    public bool Simulate(Field _simulatedField)
    {
        if (_simulatedField.CheckForVictory())
            return !_simulatedField.IsPlayersTurn;

        while (_simulatedField.ContainsEmptyCell())
        {
            int column = _simulatedField.GetRandomMove();
            _simulatedField.DropInColumn(column);

            if (_simulatedField.CheckForVictory())
            {
                return _simulatedField.IsPlayersTurn;
            }

            _simulatedField.SwitchPlayer();
        }

        return true;
    }

    public void BackPropagate(bool _playersVictory)
    {
        plays++;

        if (isPlayersTurn == _playersVictory) 
            wins++;

        if (parent != null) 
            parent.BackPropagate(_playersVictory);
    }

    public Node SelectBestChild(int _nbSimulation)
    {
        float maxValue = -1;

        Node bestNode = null;
        foreach (Node child in children.Keys)
        {
            float evaluation = child.wins / child.plays + Mathf.Sqrt(2 * Mathf.Log(_nbSimulation) / child.plays);
            if (maxValue < evaluation)
            {
                maxValue = evaluation;
                bestNode = child;
            }
        }

        return bestNode;
    }

    public int MostSelectedMove()
    {
        float maxValue = -1;
        int bestMove = -1;
        foreach (KeyValuePair<Node, int> child in children)
        {
            if (child.Key.wins / child.Key.plays > maxValue)
            {
                bestMove = child.Value;
                maxValue = child.Key.wins / child.Key.plays;
            }
        }

        return bestMove; //returns the best column.
    }
}
