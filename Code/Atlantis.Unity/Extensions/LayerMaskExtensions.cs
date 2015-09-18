// -----------------------------------------------------------------------------
//  <copyright file="Extensions.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Unity.Extensions
{
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public static class LayerMaskExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public static bool Contains( this LayerMask mask, int layerMask )
        {
            return ( mask.value & ( 1 << layerMask ) ) > 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public static bool Contains( this LayerMask mask, string layerMask )
        {
            return ( mask.value & ( 1 << LayerMask.NameToLayer( layerMask ) ) ) > 0;
        }
    }
}
