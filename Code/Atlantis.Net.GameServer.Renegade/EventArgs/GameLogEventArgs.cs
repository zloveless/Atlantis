// -----------------------------------------------------------------------------
//  <copyright file="GameLogEventArgs.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.GameServer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Atlantis.Extensions;

    public class GameLogEventArgs : RenLogEventArgs
    {
        private readonly IDictionary<GameObjDataKeys, object> _data = new Dictionary<GameObjDataKeys, object>();

        private bool _parsed;
        private string _event;
        private string _objectType;
        private GameObjData _obj1;
        private GameObjData _obj2;

        public GameLogEventArgs(string message) : base(message)
        {
        }

        #region Properties

        public string Event
        {
            get
            {
                EnsureParsed();
                return _event;
            }
            private set { _event = value; }
        }

        /// <summary>
        ///     <para>Gets a value indicating what the object type is.</para>
        /// </summary>
        public string ObjectType
        {
            get
            {
                EnsureParsed();
                return _objectType;
            }
            private set { _objectType = value; }
        }

        /// <summary>
        ///     <para>Gets a reference to the first game object </para>
        /// </summary>
        public GameObjData Obj1
        {
            get
            {
                EnsureParsed();
                return _obj1;
            }
            private set { _obj1 = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public GameObjData Obj2
        {
            get
            {
                EnsureParsed();
                return _obj2;
            }
            private set { _obj2 = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyDictionary<GameObjDataKeys, object> Data => new ReadOnlyDictionary<GameObjDataKeys, object>(_data);

        /// <summary>
        ///     <para>Gets a value indicating whether the current event args are valid.</para>
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        ///     <para>Gets a value representing the validation error that occured during parsing.</para>
        /// </summary>
        public string ValidationError { get; private set; }

        #endregion

        private void EnsureParsed()
        {
            if (!_parsed)
            {
                ParseGameLog(Message);
                _parsed = true;
            }
        }
        
        private void ParseGameLog(string message)
        {
            var tokens = message.Split(';');

            if (tokens.Length < 2)
            {
                return;
            }

            _event = tokens[0];

            // Only two events that don't have an object type, for good reason.
            // ENTER & EXIT events are related to players entering/exiting vehicles
            if (!_event.EqualsIgnoreCase("ENTER")
                && !_event.EqualsIgnoreCase("EXIT"))
            {
                ObjectType = tokens[1];
            }

            try
            {
                if (_event.EqualsIgnoreCase("CREATED"))
                {
                    /*
                    "CREATED;%s;%d;%s;%d;%d;%d;%d;%d;%d;%d"

                    0. CREATED
                    1. ObjectType
                    2. obj->Get_ID()
                    3. obj->Get_Definition().Get_Name()
                    4. (int)Pos.Y
                    5. (int)Pos.X,
                    6. (int)Pos.Z
                    7. (int)Commands->Get_Facing(obj),
                    8. (int)Commands->Get_Max_Health(obj)
                    9. (int)Commands->Get_Max_Shield_Strength(obj)
                    10. Get_Object_Type(obj)
                    */

                    Obj1 = new GameObjData
                               {
                                   Id = int.Parse(tokens[2]),
                                   DefName = tokens[3],
                                   Y = int.Parse(tokens[4]),
                                   X = int.Parse(tokens[5]),
                                   Z = int.Parse(tokens[6]),
                                   Facing = int.Parse(tokens[7])
                               };

                    _data.Add(GameObjDataKeys.MaximumHealth, int.Parse(tokens[8]));
                    _data.Add(GameObjDataKeys.MaximumShield, int.Parse(tokens[9]));
                    _data.Add(GameObjDataKeys.ObjectType, int.Parse(tokens[10]));
                }
                else if (_event.EqualsIgnoreCase("ENTER") || _event.EqualsIgnoreCase("EXIT"))
                {
                    /*
                    "ENTER;%d;%s;%d;%d;%d;%d;%s;%d;%d;%d"

                    0. ENTER
                    1. Vehicle->Get_ID()
                    2. Vehicle->Get_Definition().Get_Name()
                    3. (int)VehiclePos.Y
                    4. (int)VehiclePos.X
                    5. (int)VehiclePos.Z
                    6. Soldier->Get_ID()
                    7. Soldier->Get_Definition().Get_Name()
                    8. (int)SoldierPos.Y
                    9. (int)SoldierPos.Y
                    10. (int)SoldierPos.Y
                    *//*
                    "EXIT;%d;%s;%d;%d;%d;%d;%s;%d;%d;%d"

                    0. EXIT
                    1. Vehicle->Get_ID()
                    2. Vehicle->Get_Definition().Get_Name()
                    3. (int)VehiclePos.Y
                    4. (int)VehiclePos.X
                    5. (int)VehiclePos.Z
                    6. Soldier->Get_ID()
                    7. Soldier->Get_Definition().Get_Name()
                    8. (int)SoldierPos.Y
                    9. (int)SoldierPos.Y
                    10. (int)SoldierPos.Y
                    */

                    Obj1 = new GameObjData()
                               {
                                   Id = int.Parse(tokens[1]),
                                   DefName = tokens[2],
                                   Y = int.Parse(tokens[3]),
                                   X = int.Parse(tokens[4]),
                                   Z = int.Parse(tokens[5]),
                               };

                    Obj2 = new GameObjData()
                               {
                                   Id = int.Parse(tokens[6]),
                                   DefName = tokens[7],
                                   Y = int.Parse(tokens[8]),
                                   X = int.Parse(tokens[9]),
                                   Z = int.Parse(tokens[10])
                               };
                }
                else if (_event.EqualsIgnoreCase("DAMAGED"))
                {
                    /*
                    "DAMAGED;%s;%d;%s;%d;%d;%d;%d;%d;%s;%d;%d;%d;%d;%f;%d;%d;%d",
            
                    0. DAMAGED
                    1. ObjectType
                    2. Victim->Get_ID()
                    3. Victim->Get_Definition().Get_Name()
                    4. (int)VictimPos.Y
                    5. (int)VictimPos.X
                    6. (int)VictimPos.Z
                    7. (int)Commands->Get_Facing(Victim)
                    8. Damager?Damager->Get_ID():0
                    9. Damager?Damager->Get_Definition().Get_Name():""
                    10. (int)DamagerPos.Y
                    11. (int)DamagerPos.X
                    12. (int)DamagerPos.Z
                    13. (int)Commands->Get_Facing(Damager)
                    14. Damage
                    15. (int)Victim->Get_Defense_Object()->Get_Health()
                    16. (int)Victim->Get_Defense_Object()->Get_Shield_Strength()
                    17. (int)Commands->Get_Points(Damager)
                    */

                    Obj1 = new GameObjData
                               {
                                   Id = int.Parse(tokens[2]),
                                   DefName = tokens[3],
                                   Y = int.Parse(tokens[4]),
                                   X = int.Parse(tokens[5]),
                                   Z = int.Parse(tokens[6]),
                                   Facing = int.Parse(tokens[7])
                               };

                    Obj2 = new GameObjData
                               {
                                   Id = int.Parse(tokens[8]),
                                   DefName = tokens[9],
                                   Y = int.Parse(tokens[10]),
                                   X = int.Parse(tokens[11]),
                                   Z = int.Parse(tokens[12]),
                                   Facing = int.Parse(tokens[13])
                               };

                    _data.Add(GameObjDataKeys.Damage, int.Parse(tokens[14]));
                    _data.Add(GameObjDataKeys.Health, int.Parse(tokens[15]));
                    _data.Add(GameObjDataKeys.Shield, int.Parse(tokens[16]));
                    _data.Add(GameObjDataKeys.Points, int.Parse(tokens[17]));
                }
                else if (_event.EqualsIgnoreCase("KILLED"))
                {
                    /*
                    "KILLED;%s;%d;%s;%d;%d;%d;%d;%d;%s;%d;%d;%d;%d;%s;%s;%s"
            
                    0. KILLED
                    1. ObjectType
                    2. Victim->Get_ID()
                    3. Victim->Get_Definition().Get_Name()
                    4. (int)VictimPos.Y
                    5. (int)VictimPos.X
                    6. (int)VictimPos.Z
                    7. (int)Commands->Get_Facing(Victim)
                    8. Killer?Killer->Get_ID():0
                    9. Killer?Killer->Get_Definition().Get_Name():""
                    10. (int)KillerPos.Y
                    11. (int)KillerPos.X
                    12. (int)KillerPos.Z
                    13. (int)Commands->Get_Facing(Killer)
                    14. Get_Current_Weapon(Killer)
                    15. TranslationVictim
                    16. DATranslationManager::Translate(Killer)
                    */

                    Obj1 = new GameObjData
                               {
                                   Id = int.Parse(tokens[2]),
                                   DefName = tokens[3],
                                   Y = int.Parse(tokens[4]),
                                   X = int.Parse(tokens[5]),
                                   Z = int.Parse(tokens[6]),
                                   Facing = int.Parse(tokens[7])
                               };

                    Obj2 = new GameObjData
                               {
                                   Id = int.Parse(tokens[8]),
                                   DefName = tokens[9],
                                   Y = int.Parse(tokens[10]),
                                   X = int.Parse(tokens[11]),
                                   Z = int.Parse(tokens[12]),
                                   Facing = int.Parse(tokens[13])
                               };

                    _data.Add(GameObjDataKeys.CurrentWeapon, tokens[14]);
                    _data.Add(GameObjDataKeys.TranslationVictim, tokens[15]);
                    _data.Add(GameObjDataKeys.TranslationKiller, tokens[16]);
                }
                else if (_event.EqualsIgnoreCase("DESTROYED"))
                {
                    /*
                    "DESTROYED;%s;%d;%s;%d;%d;%d"

                    0. DESTROYED
                    1. ObjectType
                    2. obj->Get_ID()
                    3. obj->Get_Definition().Get_Name()
                    4. (int)Pos.Y
                    5. (int)Pos.X
                    6. (int)Pos.Z
                    */

                    Obj1 = new GameObjData
                               {
                                   Id = int.Parse(tokens[2]),
                                   DefName = tokens[3],
                                   Y = int.Parse(tokens[4]),
                                   X = int.Parse(tokens[5]),
                                   Z = int.Parse(tokens[6])
                               };
                }

                IsValid = true;
            }
            catch (FormatException)
            {
                IsValid = false;

                // TODO: Re-evaluate this for readability in future. Replace with string.Format if unreadable.
                ValidationError = $"Invalid format of log string: {Message}";
            }
        }
    }
}
