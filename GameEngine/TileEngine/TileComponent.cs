﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameEngine.CameraEngine;

namespace GameEngine.TileEngine
{
    public class TileComponent: DrawableGameComponent
    {
        private Camera2D camera;
        private TileScene tileScene;
        public TileScene TileScene
        {
            get { return tileScene; }
        }

        private Vector2 baseScreenSize;
        public Vector2 BaseScreenSize
        {
            get { return baseScreenSize; }
            set { baseScreenSize = value; }
        }

        public Boolean ResultionIndependent { get; set; }

        public TileComponent(Game game, TileScene tileScene, Vector2 baseScreenSize, bool resultionIndependent)
            :base(game)
        {
            this.tileScene = tileScene;
            this.baseScreenSize = baseScreenSize;
            this.ResultionIndependent = resultionIndependent;
        }

        public override void Initialize()
        {
            camera = this.Game.Services.GetService(typeof(Camera2D)) as Camera2D;
            foreach (TileLayer layer in tileScene.Layers)
            {
                TileLayer currentLayer = layer;
                this.CorrectCamera(ref camera, ref currentLayer);
                
            }
        }

        public override void Update(GameTime gameTime)
        {
            Camera2D camera = this.Game.Services.GetService(typeof(Camera2D)) as Camera2D;
            foreach (TileLayer layer in tileScene.Layers)
            {
                if (!layer.CameraIndependent)
                {
                    if (camera.Changed)
                    {
                        TileLayer currentLayer = layer;
                        this.CorrectCamera(ref camera, ref currentLayer);
                    }
                }
                else{
                        TileLayer currentLayer = layer;
                        this.MoveLayer(ref currentLayer);           
                }
            }



            //if (camera.Changed)
            //{
            //    foreach (TileLayer layer in tileScene.Layers)
            //    {

            //    }
            //    camera.Changed = false;
            //}
        }

        public override void Draw(GameTime gameTime)
        {

            SpriteBatch spriteBatch = Game.Services.GetService(typeof(SpriteBatch)) as SpriteBatch;
            Vector3 screenScalingFactor;
            Rectangle clientBounds;
            if (this.ResultionIndependent)
            {
                float horScaling = (float)this.GraphicsDevice.PresentationParameters.BackBufferWidth / baseScreenSize.X;
                float verScaling = (float)this.GraphicsDevice.PresentationParameters.BackBufferHeight / baseScreenSize.Y;
                screenScalingFactor = new Vector3(horScaling, verScaling, 1);
                clientBounds = new Rectangle(0, 0, (int)this.baseScreenSize.X, (int)this.baseScreenSize.Y);
            }
            else
            {
                screenScalingFactor = new Vector3(1, 1, 1);
                clientBounds = new Rectangle(0, 0, (int)this.GraphicsDevice.Viewport.Width, (int)this.GraphicsDevice.Viewport.Height);

            }
            Matrix globalTransformation = Matrix.CreateScale(screenScalingFactor);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, globalTransformation);

            foreach (TileLayer tileLayer in tileScene.Layers)
            {
                if (tileLayer.Visible)
                {
                    Point layerLenght = tileLayer.Lenght;
                    int layerWidth = layerLenght.X;
                    int layerHeight = layerLenght.Y;
                    TileMap tileMap = tileLayer.TileMap;


                    TileDrawLimits drawLimits = this.GetDrawLimits(tileLayer, clientBounds);

                    TileCatalog tileCatalog = tileLayer.TileCatalog;
                    for (int j = drawLimits.YMin; j < drawLimits.YMax; j++)
                    {
                        for (int i = drawLimits.XMin; i < drawLimits.XMax; i++)
                        {

                            Rectangle sourceTile = tileCatalog.TilePositions[tileMap.GetTile(i, j).Index];
                            //calcular la posicion de cada tile donde corresponde, multiplicando el numero de turno por su tamaño
                            //pe. x = 5 * 10 = 50
                            Vector2 tileAbsolutePosition = Vector2.Multiply( Vector2.Multiply(new Vector2(i, j), tileLayer.ScaledTileSize),camera.Zoom) ;
                            //calcular la posicion con respecto a la posicion de la capa
                            Vector2 tileRelativePosition = Vector2.Add(tileLayer.Position, tileAbsolutePosition);

                            spriteBatch.Draw(tileCatalog.Texture,
                                             tileRelativePosition,
                                             sourceTile,
                                             Color.White * tileLayer.transparency,
                                             0, Vector2.Zero,
                                             Vector2.Multiply(camera.Zoom, tileLayer.ZoomScale),
                                             SpriteEffects.None,
                                             0.0f);
                        }
                    }

                }
            }
            spriteBatch.End();
            base.Draw(gameTime);
        }

        public TileDrawLimits GetDrawLimits(TileLayer layer, Rectangle clientBounds)
        {
            int[] xLimit = this.GetAxisLimit(layer.Position.X, layer.ScaledSize.X * camera.Zoom.X, layer.ScaledTileSize.X * camera.Zoom.X, layer.Lenght.X, clientBounds.Width);
            int[] yLimit = this.GetAxisLimit(layer.Position.Y, layer.ScaledSize.Y * camera.Zoom.Y, layer.ScaledTileSize.Y * camera.Zoom.Y, layer.Lenght.Y, clientBounds.Height);
            TileDrawLimits tileDrawLimits = new TileDrawLimits(xLimit[0], xLimit[1], yLimit[0], yLimit[1]);
            return tileDrawLimits;
        }

        public int[] GetAxisLimit(float position, float zoomedScaledSize, float zoomedScaledTileSize, int lenght, int bound)
        {
            int min = 0;
            int max = 0;
            if (position < 0 && ((position + zoomedScaledSize) > 0 && (position + zoomedScaledSize) <= bound))
            {
                min = (int)(-position / zoomedScaledTileSize);
                max = (int)((position + zoomedScaledSize) / (zoomedScaledTileSize)) + min + 2;
            }
            else if (position >= 0 && ((position + zoomedScaledSize) <= bound))
            {
                min = 0;
                max = lenght;
            }
            else if (position >= 0 && ((position + zoomedScaledSize) > bound))
            {
                min = 0;
                max = (int)((bound - position) / zoomedScaledTileSize) + 2;
            }
            else if (position < 0 && ((position + zoomedScaledSize) > bound))
            {
                min = (int)(-position / zoomedScaledTileSize);
                max = (int)(bound / zoomedScaledTileSize) + min + 2;
            }
            min = (int)MathHelper.Clamp(min, 0, lenght);
            max = (int)MathHelper.Clamp(max, 0, lenght);

            return new int[] { min, max };
        }

        public void CorrectCamera(ref Camera2D camera, ref TileLayer layer)
        {
            Vector2 displacementRatio = Vector2.Divide(layer.Velocity, camera.Velocity);
            Vector2 realDisplacement = Vector2.Multiply(camera.Position, displacementRatio);
            Vector2 realPosition = Vector2.Subtract(layer.Origin, realDisplacement);
            layer.Position = realPosition;
        }

        public void MoveLayer(ref TileLayer layer)
        {
            switch (layer.Direction)
            {
                case TileLayerMovementDirection.Up:
                    layer.Position = Vector2.Add(layer.Position, new Vector2(0, -layer.Velocity.Y));
                    break;
                case TileLayerMovementDirection.Down:
                    layer.Position = Vector2.Add(layer.Position, new Vector2(0, layer.Velocity.Y));
                    break;
                case TileLayerMovementDirection.Left:
                    layer.Position = Vector2.Add(layer.Position, new Vector2(-layer.Velocity.X, 0));
                    break;
                case TileLayerMovementDirection.Right:
                    layer.Position = Vector2.Add(layer.Position, new Vector2(layer.Velocity.X, 0));
                    break;
                case TileLayerMovementDirection.LowerLeft:
                    layer.Position = Vector2.Add(layer.Position, new Vector2(-layer.Velocity.X, layer.Velocity.Y));
                    break;
                case TileLayerMovementDirection.LowerRigth:
                    layer.Position = Vector2.Add(layer.Position, new Vector2(layer.Velocity.X, layer.Velocity.Y));
                    break;
                case TileLayerMovementDirection.UpperLeft:
                    layer.Position = Vector2.Add(layer.Position, new Vector2(-layer.Velocity.X, -layer.Velocity.Y));
                    break;
                case TileLayerMovementDirection.UpperRight:
                    layer.Position = Vector2.Add(layer.Position, new Vector2(layer.Velocity.X, -layer.Velocity.Y));
                    break;
            }
        }

    }
}
