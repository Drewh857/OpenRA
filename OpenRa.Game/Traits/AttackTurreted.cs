﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	abstract class AttackBase : IOrder
	{
		public Actor target;

		// time (in frames) until each weapon can fire again.
		protected int primaryFireDelay = 0;
		protected int secondaryFireDelay = 0;

		protected bool CanAttack( Actor self )
		{
			if( primaryFireDelay > 0 ) --primaryFireDelay;
			if( secondaryFireDelay > 0 ) --secondaryFireDelay;

			if( target != null && target.IsDead ) target = null;		/* he's dead, jim. */
			if( target == null ) return false;

			return true;
		}

		protected void DoAttack( Actor self )
		{
			if( self.unitInfo.Primary != null && CheckFire( self, self.unitInfo.Primary, ref primaryFireDelay, self.unitInfo.PrimaryOffset ) )
			{
				secondaryFireDelay = Math.Max( 4, secondaryFireDelay );
				return;
			}
			if( self.unitInfo.Secondary != null && CheckFire( self, self.unitInfo.Secondary, ref secondaryFireDelay, 
				self.unitInfo.SecondaryOffset ?? self.unitInfo.PrimaryOffset) )
				return;
		}

		bool CheckFire( Actor self, string weaponName, ref int fireDelay, int[] offset )
		{
			if( fireDelay > 0 ) return false;
			var weapon = Rules.WeaponInfo[ weaponName ];
			if( weapon.Range * weapon.Range < ( target.Location - self.Location ).LengthSquared ) return false;

			fireDelay = weapon.ROF;

			Game.world.Add( new Bullet( weaponName, self.Owner, self,
				self.CenterLocation.ToInt2() + Util.GetTurretPosition( self, offset ),
				target.CenterLocation.ToInt2() ) );

			return true;
		}

		public Order Order( Actor self, int2 xy, bool lmb, Actor underCursor )
		{
			if( lmb || underCursor == null ) return null;

			if( underCursor.Owner == self.Owner ) return null;

			return OpenRa.Game.Order.Attack( self, underCursor );
		}
	}

	class AttackTurreted : AttackBase, ITick
	{
		public AttackTurreted( Actor self ) { self.traits.Get<Turreted>(); }

		public void Tick(Actor self)
		{
			if( !CanAttack( self ) ) return;

			var turreted = self.traits.Get<Turreted>();
			turreted.desiredFacing = Util.GetFacing( target.CenterLocation - self.CenterLocation, turreted.turretFacing );
			if( turreted.desiredFacing != turreted.turretFacing )
				return;

			DoAttack( self );
		}
	}
}
