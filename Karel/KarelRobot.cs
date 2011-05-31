﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using InVision.Extensions;
using InVision.Framework.Components.Actions;
using InVision.GameMath;
using InVision.Ogre;

namespace Karel
{
	public class KarelRobot : KarelWorldComponent
	{
		private readonly ConcurrentQueue<UpdateAction> _pendingActions;
		private SceneNode _sceneNode;

		/// <summary>
		/// Initializes a new instance of the <see cref="KarelRobot"/> class.
		/// </summary>
		public KarelRobot()
		{
			_pendingActions = new ConcurrentQueue<UpdateAction>();
			RepeatUpdateSteps = true;
			IsOff = true;
		}

		/// <summary>
		/// Gets a value indicating whether this instance has pending actions.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has pending actions; otherwise, <c>false</c>.
		/// </value>
		public bool HasPendingActions
		{
			get { return _pendingActions.Count > 0; }
		}

		/// <summary>
		/// Gets or sets the direction.
		/// </summary>
		/// <value>The direction.</value>
		public KarelDirection Direction { get; private set; }

		/// <summary>
		/// Gets a value indicating whether [looking to north].
		/// </summary>
		/// <value><c>true</c> if [looking to north]; otherwise, <c>false</c>.</value>
		public bool LookingToNorth
		{
			get { return LookingTo(KarelDirection.North); }
		}

		/// <summary>
		/// Gets a value indicating whether [looking to west].
		/// </summary>
		/// <value><c>true</c> if [looking to west]; otherwise, <c>false</c>.</value>
		public bool LookingToWest
		{
			get { return LookingTo(KarelDirection.West); }
		}

		/// <summary>
		/// Gets a value indicating whether [looking to east].
		/// </summary>
		/// <value><c>true</c> if [looking to east]; otherwise, <c>false</c>.</value>
		public bool LookingToEast
		{
			get { return LookingTo(KarelDirection.East); }
		}

		/// <summary>
		/// Gets a value indicating whether [looking to south].
		/// </summary>
		/// <value><c>true</c> if [looking to south]; otherwise, <c>false</c>.</value>
		public bool LookingToSouth
		{
			get { return LookingTo(KarelDirection.South); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance has beeper.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has beeper; otherwise, <c>false</c>.
		/// </value>
		public bool HasBeeper { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is off.
		/// </summary>
		/// <value><c>true</c> if this instance is off; otherwise, <c>false</c>.</value>
		public bool IsOff { get; private set; }

		/// <summary>
		/// Lookings to.
		/// </summary>
		/// <param name="direction">The direction.</param>
		/// <returns></returns>
		public bool LookingTo(KarelDirection direction)
		{
			return Direction == direction;
		}

		/// <summary>
		/// Determines whether [has found beeper].
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if [has found beeper]; otherwise, <c>false</c>.
		/// </returns>
		public bool HasFoundBeeper()
		{
			return false;
		}

		/// <summary>
		/// Determines whether this instance can move.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if this instance can move; otherwise, <c>false</c>.
		/// </returns>
		public bool CanMove()
		{
			return true;
		}

		/// <summary>
		/// Turns the on.
		/// </summary>
		public void TurnOn()
		{
			_pendingActions.EnqueueAll(GetTurnOnActions());
		}

		/// <summary>
		/// Gets the turn on actions.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<UpdateAction> GetTurnOnActions()
		{
			yield return WaitBy(1.Seconds());

			Console.WriteLine("Karel turned off");
		}

		/// <summary>
		/// Turns the off.
		/// </summary>
		public void TurnOff()
		{
			_pendingActions.EnqueueAll(GetTurnOffActions());
		}

		/// <summary>
		/// Gets the turn off actions.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<UpdateAction> GetTurnOffActions()
		{
			yield return WaitBy(1.Seconds());

			Console.WriteLine("Karel turned off");
		}

		/// <summary>
		/// Moves this instance.
		/// </summary>
		public void Move()
		{
			_pendingActions.EnqueueAll(GetMoveActions());
		}

		/// <summary>
		/// Gets the move actions.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<UpdateAction> GetMoveActions()
		{
			yield return WaitBy(1.Seconds());

			var matrix = Matrix.CreateFromQuaternion(_sceneNode.Orientation);
			_sceneNode.Position += matrix.Forward;

			Console.WriteLine("Karel moved");
		}

		/// <summary>
		/// Turns the left.
		/// </summary>
		public void TurnLeft()
		{
			_pendingActions.EnqueueAll(GetTurnLeftActions());
		}

		/// <summary>
		/// Gets the turn left actions.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<UpdateAction> GetTurnLeftActions()
		{
			yield return WaitBy(1.Seconds());

			var matrix = Matrix.CreateFromQuaternion(_sceneNode.Orientation);
			matrix *= Matrix.CreateRotationY((float) (Math.PI / 2f));
			//_sceneNode.Position += matrix.Forward;
			Vector3 scale;
			Quaternion rotation;
			Vector3 translation;
			
			matrix.Decompose(out scale, out rotation, out translation);
			_sceneNode.Orientation = rotation;

			Console.WriteLine("Karel Turned left");
		}

		/// <summary>
		/// Picks the beeper.
		/// </summary>
		public void PickBeeper()
		{
			_pendingActions.EnqueueAll(GetPickBeeperActions());
		}

		/// <summary>
		/// Gets the pick beeper actions.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<UpdateAction> GetPickBeeperActions()
		{
			yield return WaitBy(1.Seconds());

			Console.WriteLine("Karel picked beeper");
		}

		/// <summary>
		/// Puts the beeper.
		/// </summary>
		public void PutBeeper()
		{
			_pendingActions.EnqueueAll(GetPutBeeperActions());
		}

		/// <summary>
		/// Gets the put beeper actions.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<UpdateAction> GetPutBeeperActions()
		{
			yield return WaitBy(1.Seconds());

			Console.WriteLine("Karel put beeper");
		}

		/// <summary>
		/// Updates the by steps.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<UpdateAction> UpdateBySteps()
		{
			while (!_pendingActions.IsEmpty) {
				UpdateAction action;
				_pendingActions.TryDequeue(out action);
				yield return action;
			}
		}

		/// <summary>
		/// Initializes the self.
		/// </summary>
		/// <param name="app">The app.</param>
		protected override void InitializeSelf(InVision.Framework.GameApplication app)
		{
			dynamic ogre = GameApplication.GlobalVariables.Ogre;

			var sceneManager = (SceneManager)ogre.SceneManager;
			var worldSceneNode = (SceneNode)StateVariables.WorldSceneNode;

			var robot = sceneManager.CreateEntity("robot.mesh");
			_sceneNode = worldSceneNode.CreateChildSceneNode();
			_sceneNode.AttachObject(robot);
			_sceneNode.Position = new Vector3(WorldPosition.X, 0, WorldPosition.Y);
			_sceneNode.Scale = new Vector3(0.02f, 0.02f, 0.02f);

		}
	}
}