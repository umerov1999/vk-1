using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using VkNet.Enums.SafetyEnums;
using VkNet.Exception;
using VkNet.Utils;

namespace VkNet.Model.Keyboard
{
	/// <inheritdoc />
	[Serializable]
	[UsedImplicitly]
	public class KeyboardBuilder : IKeyboardBuilder
	{
		private bool IsOneTime { get; set; }

		private bool IsInline { get; set; }

		private readonly List<List<MessageKeyboardButton>> _fullKeyboard = new List<List<MessageKeyboardButton>>();

		private List<MessageKeyboardButton> _currentLine = new List<MessageKeyboardButton>();

		private readonly string _type;

		private const string Button = "button";

		/// <inheritdoc />
		public KeyboardBuilder() : this(Button)
		{
		}

		/// <inheritdoc />
		public KeyboardBuilder(bool isOneTime) : this(Button, isOneTime)
		{
		}

		/// <inheritdoc />
		public KeyboardBuilder(string type, bool isOneTime = false)
		{
			IsOneTime = isOneTime;
			_type = type;
		}

		/// <inheritdoc />
		public IKeyboardBuilder AddButton(string label, string extra, KeyboardButtonColor color = default,
										string type = null)
		{
			color ??= KeyboardButtonColor.Default;
			type ??= _type ?? Button;
			var payload = $"{{\"{type}\":\"{extra}\"}}";

			if (payload.Length > 255 && type == Button)
			{
				throw new VkKeyboardPayloadMaxLengthException("Для кнопки payload должен быть максимум 255 символов: " + payload);
			}

			_currentLine.Add(new MessageKeyboardButton
			{
				Color = color,
				Action = new MessageKeyboardButtonAction
				{
					Label = label,
					Payload = payload,
					Type = KeyboardButtonActionType.Text
				}
			});

			return this;
		}

		/// <inheritdoc />
		public IKeyboardBuilder AddLine()
		{
			_fullKeyboard.Add(_currentLine);
			_currentLine = new List<MessageKeyboardButton>();

			return this;
		}

		/// <inheritdoc />
		public IKeyboardBuilder SetOneTime()
		{
			IsOneTime = true;

			return this;
		}

		/// <inheritdoc />
		public IKeyboardBuilder SetInline(bool inline = true)
		{
			IsInline = inline;

			return this;
		}

		/// <inheritdoc />
		public IKeyboardBuilder Clear()
		{
			_currentLine.Clear();
			_fullKeyboard.ForEach(x => x.Clear());
			_fullKeyboard.Clear();

			return this;
		}

		/// <inheritdoc />
		public MessageKeyboard Build()
		{
			if (_currentLine.Count != 0)
			{
				_fullKeyboard.Add(_currentLine);
			}

			var totalPayloadLength = _fullKeyboard.Aggregate(0,
				(current, list) =>
					list.Any() ? list.Aggregate(current, (length, button) => length + button.Action.Payload.Length) : current);

			if (totalPayloadLength > 1000)
			{
				throw new VkKeyboardPayloadMaxLengthException(
					"Суммарная длина для payload всех кнопок должен быть максимум 1000 символов.");
			}

			return new MessageKeyboard
			{
				OneTime = IsOneTime,
				Inline = IsInline,
				Buttons = _fullKeyboard.Select(e => e.ToReadOnlyCollection()).ToReadOnlyCollection()
			};
		}
	}
}