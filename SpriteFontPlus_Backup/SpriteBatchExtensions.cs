using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteFontPlus
{
	public static class SpriteBatchExtensions
	{
		public static float DrawString(this SpriteBatch batch, DynamicSpriteFont font,
			string _string_, Vector2 pos, Color color)
		{
			return font.DrawString(batch, _string_, pos, color);
		}

		public static float DrawString(this SpriteBatch batch, DynamicSpriteFont font,
			string _string_, Vector2 pos, Color color, Vector2 scale)
		{
			return font.DrawString(batch, _string_, pos, color, scale, 0f);
		}

		// [新增] 兼容11参数的DrawString重载
		public static void DrawString(this SpriteBatch batch, SpriteFont font, string text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth, bool rtl)
		{
			// 忽略rtl参数，使用标准DrawString
			batch.DrawString(font, text, position, color, rotation, origin, scale, effects, layerDepth);
		}

		// [新增] 兼容DynamicSpriteFont的9参数重载
		public static void DrawString(this SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
		{
			// DynamicSpriteFont只支持简单的DrawString，忽略rotation, origin, effects参数
			font.DrawString(batch, text, position, color, new Vector2(scale), layerDepth);
		}

		// [新增] 兼容DynamicSpriteFont的9参数重载（Vector2 scale版本）
		public static void DrawString(this SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
		{
			// DynamicSpriteFont只支持简单的DrawString，忽略rotation, origin, effects参数
			font.DrawString(batch, text, position, color, scale, layerDepth);
		}
	}
}