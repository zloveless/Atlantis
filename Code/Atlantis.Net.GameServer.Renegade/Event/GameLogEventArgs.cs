// -----------------------------------------------------------------------------
//  <copyright file="GameLogEventArgs.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.GameServer
{
    public class GameLogEventArgs : RenLogEventArgs
    {
        public GameLogEventArgs(string message) : base(message)
        {
            ParseGameLog(message);
        }

        public string Event { get; private set; }

        public string ObjectType { get; private set; }

        public int ID1 { get; private set; }

        public int ID2 { get; private set; }

        private void ParseGameLog(string message)
        {
            var tokens = message.Split(';');

            if (tokens.Length < 2)
            {
                return;
            }

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
            */

            /*
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

            /*
            "KILLED;%s;%d;%s;%d;%d;%d;%d;%d;%s;%d;%d;%d;%d;%s;%s;%s"
            
            0. ObjectType
            1. Victim->Get_ID()
            2. Victim->Get_Definition().Get_Name()
            3. (int)VictimPos.Y
            4. (int)VictimPos.X
            5. (int)VictimPos.Z
            6. (int)Commands->Get_Facing(Victim)
            7. Killer?Killer->Get_ID():0
            8. Killer?Killer->Get_Definition().Get_Name():""
            9. (int)KillerPos.Y
            10. (int)KillerPos.X
            11. (int)KillerPos.Z
            12. (int)Commands->Get_Facing(Killer)
            13. Get_Current_Weapon(Killer)
            14. Translation
            15. DATranslationManager::Translate(Killer)
            */

            /*
            "DESTROYED;%s;%d;%s;%d;%d;%d"
            
            0. ObjectType
            1. obj->Get_ID()
            2. obj->Get_Definition().Get_Name()
            3. (int)Pos.Y
            4. (int)Pos.X
            5. (int)Pos.Z
            */
        }
    }
}
