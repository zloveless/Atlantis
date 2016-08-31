// -----------------------------------------------------------------------------
//  <copyright file="GameObjData.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.GameServer
{
    public class GameObjData
    {
        /// <summary>
        ///     <para>Gets the ID number of the current game object.</para>
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     <para>Gets the definition name of the current game object.</para>
        /// </summary>
        public string DefName { get; set; }

        /// <summary>
        ///     <para>Gets the X value of the current game object.</para>
        /// </summary>
        public int X { get; set; }

        /// <summary>
        ///     <para>Gets the Y value of the current game object.</para>
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        ///     <para>Gets the Z value of the current game object.</para>
        /// </summary>
        public int Z { get; set; }

        /// <summary>
        ///     <para>Gets the direction the current game object is facing.</para>
        /// </summary>
        public int Facing { get; set; }
    }
}
