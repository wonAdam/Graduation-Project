﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class APUnit
{
    public Unit owner;

    public int health;
    public int actionPoint;
    public bool isSelf;

    public APUnit(Unit unit, bool isSelf)
    {
        owner = unit;
        health = unit.Health;
        actionPoint = unit.actionPointsRemain;
        this.isSelf = isSelf;
    }

    public APUnit(APUnit apUnit)
    {
        owner = apUnit.owner;
        //cube = apUnit.cube;
        health = apUnit.health;
        actionPoint = apUnit.actionPoint;
        isSelf = apUnit.isSelf;

    }

}


public class APGameState
{
    public List<Cube> _cubes = new List<Cube>();
    public List<APUnit> _units = new List<APUnit>();
    public Dictionary<APUnit, Cube> _unitPos = new Dictionary<APUnit, Cube>();
    public APUnit self { get => _units.Find(u => u.isSelf); }
    public APGameState(Unit self, List<Unit> units, List<Cube> cubes)
    {
        // init APUnit List
        foreach(var unit in units)
            _units.Add(new APUnit(unit, unit == self ? true : false));

        // init Cube List
        _cubes.AddRange(cubes);

        // set position
        foreach(var unit in units)
        {
            Cube cube = unit.GetCube;
            APUnit apUnit = _units.Find(u => u.owner == unit);

            _unitPos.Add(apUnit, cube);
        }
    }

    private APGameState(APUnit self, List<APUnit> units, List<Cube> cubes, Dictionary<APUnit, Cube> unitPos)
    {
        // init APUnit List
        foreach(var apUnit in units)
            _units.Add(new APUnit(apUnit));

        // init Cube List
        _cubes.AddRange(cubes);

        // set position
        foreach (var pair in unitPos)
        {
            APUnit unit = pair.Key;
            APUnit myApUnit = _units.Find(u => u.owner == unit.owner);
            _unitPos.Add(myApUnit, pair.Value);
        }
    }

    public APGameState Clone() => new APGameState(self, _units, _cubes, _unitPos);
    public Cube APFind(INavable cube) => _cubes.Find(c => c == cube as Cube);
    public APUnit APFind(Unit unit) => _units.Find(u => u.owner == unit);

    public void MoveTo(Cube destination)
    {
        _unitPos.Remove(self);
        this._unitPos.Add(self, destination);
    }

    public void Attack(APUnit target)
    {
        if (target == null)
            return;

        target.health -= self.owner.BasicAttackDamageAvg;

        if(target.health <= 0)
        {
            Cube targetCube;
            if(_unitPos.TryGetValue(target, out targetCube))
            {
                _units.Remove(target);
                _unitPos.Remove(target);
            }
        }
    }
}
