// -----------------------------------------------------------------------------
//  <copyright file="Extensions.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Unity
{
    using UnityEngine;

    public static class Extensions
    {
        public static bool Contains( this LayerMask mask, int layerMask )
        {
            return ( mask.value & ( 1 << layerMask ) ) > 0;
        }

        public static bool Contains( this LayerMask mask, string layerMask )
        {
            return ( mask.value & ( 1 << LayerMask.NameToLayer( layerMask ) ) ) > 0;
        }
    }
}
