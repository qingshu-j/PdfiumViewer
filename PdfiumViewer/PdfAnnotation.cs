using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;

namespace PdfiumViewer
{
    public class PdfAnnotation
    {
        private readonly PdfViewer _pdfViewer;
        private List<PointF> _currentStroke = new List<PointF>();
        private Bitmap _bufferBitmap;
        private Graphics _bufferGraphics;

        public PdfAnnotation(PdfViewer pdfViewer)
        {
            _pdfViewer = pdfViewer;
            var renderPanel = pdfViewer.GetChildAtPoint(Point.Empty) as Panel;
            if (renderPanel == null) return;

            // 初始化双缓冲
            renderPanel.Paint += OnRenderPanelPaint;
            renderPanel.MouseDown += OnMouseDown;
            renderPanel.MouseMove += OnMouseMove;
            renderPanel.MouseUp += OnMouseUp;

            _bufferBitmap = new Bitmap(renderPanel.Width, renderPanel.Height);
            _bufferGraphics = Graphics.FromImage(_bufferBitmap);
        }

        // 坐标转换：屏幕坐标 → PDF页面坐标
        private PointF ScreenToPdf(Point screenPoint, int pageIndex)
        {
            float page_width = _pdfViewer.Document.PageSizes[pageIndex].Width;
            float page_height = _pdfViewer.Document.PageSizes[pageIndex].Height;

            //// 计算缩放和滚动偏移
            //float pdfX = (screenPoint.X - viewport.Left) / viewport.Width * page_width;
            //float pdfY = (screenPoint.Y - viewport.Top) / viewport.Height * page_height;

            //// 转换坐标系（PDF原点在左下角）
            //return new PointF(pdfX, page_height - pdfY);
            return new PointF(screenPoint.X, page_height - screenPoint.Y);
        }

        // 鼠标按下：开始新笔画
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _currentStroke.Clear();
            _currentStroke.Add(ScreenToPdf(e.Location, _pdfViewer.Renderer.Page));
        }

        // 鼠标移动：记录点并实时绘制
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _currentStroke.Count == 0) return;

            // 添加新点
            _currentStroke.Add(ScreenToPdf(e.Location, _pdfViewer.Renderer.Page));

            // 在缓冲图上绘制线段
            var prevPoint = e.Location;
            if (_currentStroke.Count >= 2)
            {
                _bufferGraphics.DrawLine(Pens.Red, prevPoint, e.Location);
                ((Panel)sender).Invalidate();
            }
        }

        // 鼠标释放：生成PDF注释
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (_currentStroke.Count < 2) return;

            // 创建PDF注释
            _pdfViewer.Document.AddInkAnnotation(_pdfViewer.Renderer.Page, _currentStroke);
            _currentStroke.Clear();

            // 清空缓冲图
            _bufferGraphics.Clear(Color.Transparent);
            ((Panel)sender).Invalidate();
        }

        // 渲染双缓冲图
        private void OnRenderPanelPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(_bufferBitmap, Point.Empty);
        }

        // 保存PDF
        public void SaveDocument(string path)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            {
                _pdfViewer.Document.Save(stream);
            }
        }
    }
}


