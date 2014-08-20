using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleCritters.Helpers;

namespace SimpleCritters.Environment
{
    public interface ILayerTemplate : IResourceTemplate
    {
        new Layer Create(Habitat host);
    }
    public abstract class LayerTemplateBase : ILayerTemplate
    {
        IResource IResourceTemplate.Create(Habitat host)
        {
            return (this as ILayerTemplate).Create(host);
        }

        public abstract Layer Create(Habitat host);
    }

    public class Layer : IResource
    {
        readonly float[,] values;
        public readonly int Width;
        public readonly int Height;

        public Layer(int width, int height)
        {
            values = new float[width, height];
            Width = width;
            Height = height;
        }

        public float this[int x, int y]
        {
            get
            {
                return values[x, y];
            }
        }

        public void Invert()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    values[x, y] = 1f - values[x,y];
                }
            }
        }
        public void UniformFill(float value = 0f)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    values[x, y] = value;
                }
            }
        }
        public void BlobFill(int brushCount = 3, int brushSize = 4, int brushRuntime = 4096 * 2)
        {
            const float paintScalar = .04f;

            Random rand = new Random();

            for (int j = 0; j < brushCount; j++)
            {
                int startX = rand.Next(Width);
                int startY = rand.Next(Height);
                int posX = startX;
                int posY = startY;

                for (int i = 0; i < brushRuntime; i++)
                {
                    //Paint
                    int brushStartX = posX - brushSize / 2;
                    int brushStartY = posY - brushSize / 2;

                    for (int x = -brushSize / 2; x < brushSize / 2; x++)
                    {
                        for (int y = -brushSize / 2; y < brushSize / 2; y++)
                        {
                            float distSquared = x * x + y * y;
                            var paint = (distSquared / brushSize * brushSize) * paintScalar;

                            values[MathHelper.Wrap(posX + x, Width), MathHelper.Wrap(posY + x, Height)] += paint;
                        }
                    }

                    //Move
                    double moveDir = rand.NextDouble();

                    if (moveDir > .75d) posY--;
                    else if (moveDir > .5d) posY++;
                    else if (moveDir > .25d) posX--;
                    else posX++;

                    if (posX > Width) posX = 0;
                    if (posX < 0) posX = Width;
                    if (posY > Height) posY = 0;
                    if (posY < 0) posY = Height;
                }
            }

            //Clean-up
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (values[x, y] > 1f) 
                        values[x, y] = 1f;
                }
            }
        }
        public void NoiseFill()
        {
            Random rand = new Random();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    values[x, y] = (float)rand.NextDouble();
                }
            }
        }
        public void BlurFill(Layer source, int depth = 1, bool wrapBlur = true)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    float totalValue = 0f;
                    float area = 0;
                    for (int x2 = x - depth; x2 <= x + depth; x2++)
                    {
                        if (x2 < 0 || Width <= x2)
                            continue;

                        for (int y2 = y - depth; y2 <= y + depth; y2++)
                        {
                            if (y2 < 0 || Height <= y2)
                                continue;

                            int x3;
                            int y3;
                            if (wrapBlur)
                            {
                                x3 = MathHelper.Wrap(x2, Width);
                                y3 = MathHelper.Wrap(y2, Height);
                            }
                            else
                            {
                                x3 = x2;
                                y3 = y2;
                            }

                            totalValue += source[x3, y3];
                            area += 1f;
                        }
                    }

                    values[x, y] = totalValue / area;
                }
            }
        }

        public class UniformFillTemplate : LayerTemplateBase
        {
            readonly float fillValue;

            public UniformFillTemplate(float fillValue)
            {
                this.fillValue = fillValue;
            }

            public override Layer Create(Habitat host)
            {
                Layer result = new Layer(host.Width, host.Width);
                result.UniformFill(fillValue);
                return result;
            }
        }
        public class BlobFillTemplate : LayerTemplateBase
        {
            readonly int brushCount;
            readonly int brushSize;
            readonly int brushRuntime;

            public BlobFillTemplate(int brushCount = 3, int brushSize = 4, int brushRuntime = 4096 * 2)
            {
                this.brushCount = brushCount;
                this.brushSize = brushSize;
                this.brushRuntime = brushRuntime;
            }

            public override Layer Create(Habitat host)
            {
                Layer result = new Layer(host.Width, host.Width);
                result.BlobFill(brushCount, brushSize, brushRuntime);
                return result;
            }
        }
        public class NoiseFillTemplate : LayerTemplateBase
        {
            public override Layer Create(Habitat host)
            {
                Layer result = new Layer(host.Width, host.Width);
                result.NoiseFill();
                return result;
            }
        }
        public class BlurFillTemplate : LayerTemplateBase
        {
            readonly int depth;
            readonly bool wrapBlur;

            public BlurFillTemplate(int depth, bool wrapBlur = true)
            {
                this.depth = depth;
                this.wrapBlur = wrapBlur;
            }

            public override Layer Create(Habitat host)
            {
                Layer result = new Layer(host.Width, host.Width);
                result.BlurFill(result, depth, wrapBlur);
                return result;
            }
        }
    }
    
    public static class LayerHelper
    {
        public static KeyValuePair<string, ILayerTemplate> WithResourceName(this ILayerTemplate layerTemplate, string resourceName)
        {
            return new KeyValuePair<string, ILayerTemplate>(resourceName, layerTemplate);
        }
    }

}
