﻿using System;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// Stores the state of the Mouse
/// </summary>
public class Mouse
{
	public const int MaxButtons = 5;

	private readonly bool[] pressed = new bool[MaxButtons];
	private readonly bool[] down = new bool[MaxButtons];
	private readonly bool[] released = new bool[MaxButtons];
	private readonly TimeSpan[] timestamp = new TimeSpan[MaxButtons];
	private Vector2 wheelValue;

	/// <summary>
	/// Mouse position, relative to the window, in Pixel Coordinates.
	/// </summary>
	public Vector2 Position;

	/// <summary>
	/// Delta to the previous mouse position, in Pixel Coordinates.
	/// </summary>
	public Vector2 Delta;

	public float X
	{
		get => Position.X;
		set => Position.X = value;
	}

	public float Y
	{
		get => Position.Y;
		set => Position.Y = value;
	}

	public bool Pressed(MouseButtons button) => pressed[(int)button];
	public bool Down(MouseButtons button) => down[(int)button];
	public bool Released(MouseButtons button) => released[(int)button];

	public TimeSpan Timestamp(MouseButtons button) => timestamp[(int)button];

	public bool Repeated(MouseButtons button, float delay, float interval)
	{
		if (Pressed(button))
			return true;

		var time = timestamp[(int)button];
		return Down(button) && (Time.Duration - time).TotalSeconds > delay && Time.OnInterval(interval, time.TotalSeconds);
	}

	public bool LeftPressed => pressed[(int)MouseButtons.Left];
	public bool LeftDown => down[(int)MouseButtons.Left];
	public bool LeftReleased => released[(int)MouseButtons.Left];

	public bool RightPressed => pressed[(int)MouseButtons.Right];
	public bool RightDown => down[(int)MouseButtons.Right];
	public bool RightReleased => released[(int)MouseButtons.Right];

	public bool MiddlePressed => pressed[(int)MouseButtons.Middle];
	public bool MiddleDown => down[(int)MouseButtons.Middle];
	public bool MiddleReleased => released[(int)MouseButtons.Middle];

	public Vector2 Wheel => wheelValue;

	internal void Copy(Mouse other)
	{
		Array.Copy(other.pressed, 0, pressed, 0, MaxButtons);
		Array.Copy(other.down, 0, down, 0, MaxButtons);
		Array.Copy(other.released, 0, released, 0, MaxButtons);
		Array.Copy(other.timestamp, 0, timestamp, 0, MaxButtons);

		Position = other.Position;
		Delta = other.Delta;
		wheelValue = other.wheelValue;
	}

	internal void Step()
	{
		Array.Fill(pressed, false);
		Array.Fill(released, false);
		wheelValue = Vector2.Zero;
	}

	internal void OnButton(int buttonIndex, bool buttonPressed)
	{
		if (buttonIndex >= 0 && buttonIndex < MaxButtons)
		{
			if (buttonPressed)
			{
				down[buttonIndex] = true;
				pressed[buttonIndex] = true;
				timestamp[buttonIndex] = Time.Duration;
			}
			else
			{
				down[buttonIndex] = false;
				released[buttonIndex] = true;
			}
		}
	}

	internal void OnWheel(in Vector2 value)
		=> wheelValue = value;
}
