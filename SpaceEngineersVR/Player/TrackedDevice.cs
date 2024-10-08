﻿using SpaceEngineersVR.Util;
using System;
using Valve.VR;
using VRageMath;

namespace SpaceEngineersVR.Player
{
	public class TrackedDevice
	{
		public MatrixAndInvert deviceToAbsolute => pose.deviceToAbsolute;
		public MatrixAndInvert deviceToPlayer => new MatrixAndInvert(pose.deviceToAbsolute.matrix * Player.PlayerToAbsolute.inverted, Player.PlayerToAbsolute.matrix * pose.deviceToAbsolute.inverted);

		public struct Pose
		{
			public static readonly Pose Identity = new Pose()
			{
				isConnected = false,
				isTracked = false,
				deviceToAbsolute = MatrixAndInvert.Identity,
				velocity = Vector3.Zero,
				angularVelocity = Vector3.Zero
			};

			public bool isConnected;
			public bool isTracked;

			public MatrixAndInvert deviceToAbsolute;

			public Vector3 velocity;
			public Vector3 angularVelocity;
		}
		public Pose renderPose = Pose.Identity;
		public Pose pose = Pose.Identity;

		public uint deviceId = OpenVR.k_unTrackedDeviceIndexInvalid;

		private readonly ulong hapticsActionHandle;
		private readonly ulong actionHandle;

		public static event Action<TrackedDevice> OnDeviceConnected;
		public static event Action<TrackedDevice> OnDeviceDisconnected;
		public static event Action<TrackedDevice> OnDeviceStartTracking;
		public static event Action<TrackedDevice> OnDeviceLostTracking;

		public TrackedDevice(string actionName = null, string hapticsName = "/actions/feedback/out/GenericHaptic")
		{
			if (!string.IsNullOrEmpty(actionName))
				OpenVR.Input.GetActionHandle(actionName, ref actionHandle);
			OpenVR.Input.GetInputSourceHandle(hapticsName, ref hapticsActionHandle);
		}

		public void Vibrate(float delay, float duration, float frequency, float amplitude)
		{
			if (hapticsActionHandle == 0)
				return;

			OpenVR.Input.TriggerHapticVibrationAction(hapticsActionHandle, delay, duration, frequency, amplitude, OpenVR.k_ulInvalidInputValueHandle);
		}


		public virtual void MainUpdate()
		{
		}

		public void SetMainPoseData(TrackedDevicePose_t value)
		{
			SetPoseData(ref pose, value, out bool wasConnected, out bool wasDisconnected, out bool startedTracking, out bool lostTracking);

			if (wasConnected)
				OnConnected();
			if (wasDisconnected)
				OnDisconnected();

			if (startedTracking)
				OnStartTracking();
			if (lostTracking)
				OnLostTracking();
		}

		public void SetRenderPoseData(TrackedDevicePose_t value)
		{
			SetPoseData(ref renderPose, value, out _, out _, out _, out _);
		}

		private static void SetPoseData(ref Pose pose, TrackedDevicePose_t value, out bool wasConnected, out bool wasDisconnected, out bool startedTracking, out bool lostTracking)
		{
			wasConnected = false;
			wasDisconnected = false;
			startedTracking = false;
			lostTracking = false;

			if (pose.isConnected != value.bDeviceIsConnected)
			{
				pose.isConnected = value.bDeviceIsConnected;

				if (value.bDeviceIsConnected)
					wasConnected = true;
				else
					wasDisconnected = true;
			}

			if (pose.isTracked != value.bPoseIsValid)
			{
				pose.isTracked = value.bPoseIsValid;

				if (value.bPoseIsValid)
					startedTracking = true;
				else
					lostTracking = true;
			}

			if (pose.isTracked)
			{
				pose.deviceToAbsolute = new MatrixAndInvert(value.mDeviceToAbsoluteTracking.ToMatrix());
				pose.velocity = value.vVelocity.ToVector();
				pose.angularVelocity = value.vAngularVelocity.ToVector();
			}
		}


		protected virtual void OnConnected()
		{
			OnDeviceConnected.InvokeIfNotNull(this);
		}
		protected virtual void OnDisconnected()
		{
			OnDeviceDisconnected.InvokeIfNotNull(this);
		}

		protected virtual void OnStartTracking()
		{
			OnDeviceStartTracking.InvokeIfNotNull(this);
		}
		protected virtual void OnLostTracking()
		{
			OnDeviceLostTracking.InvokeIfNotNull(this);
		}
	}
}
