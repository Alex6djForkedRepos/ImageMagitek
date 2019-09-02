﻿using Caliburn.Micro;
using ImageMagitek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TileShop.Shared.EventModels;
using TileShop.Shared.Services;
using TileShop.WPF.Behaviors;
using TileShop.WPF.Helpers;

namespace TileShop.WPF.ViewModels
{
    public class SequentialArrangerEditorViewModel : ArrangerEditorViewModel, IMouseCaptureProxy
    {
        //public override event EventHandler Capture;
        //public override event EventHandler Release;
        private ICodecService _codecService;

        private BindableCollection<string> _codecNames = new BindableCollection<string>();
        public BindableCollection<string> CodecNames
        {
            get => _codecNames;
            set
            {
                _codecNames = value;
                NotifyOfPropertyChange(() => CodecNames);
            }
        }

        private string _selectedCodecName;
        public string SelectedCodecName
        {
            get => _selectedCodecName;
            set
            {
                _selectedCodecName = value;
                NotifyOfPropertyChange(() => SelectedCodecName);
                ChangeCodec();
            }
        }

        public bool IsLinearLayout => _arranger.Layout == ArrangerLayout.LinearArranger;
        public bool IsTiledLayout => _arranger.Layout == ArrangerLayout.TiledArranger;

        private int _tiledElementWidth = 8;
        public int TiledElementWidth
        {
            get => _tiledElementWidth;
            set
            {
                _tiledElementWidth = value;
                NotifyOfPropertyChange(() => TiledElementWidth);
                ChangeCodecDimensions(TiledElementWidth, TiledElementHeight);
            }
        }

        private int _tiledElementHeight = 8;
        public int TiledElementHeight
        {
            get => _tiledElementHeight;
            set
            {
                _tiledElementHeight = value;
                NotifyOfPropertyChange(() => TiledElementHeight);
                ChangeCodecDimensions(TiledElementWidth, TiledElementHeight);
            }
        }

        private int _tiledArrangerWidth = 8;
        public int TiledArrangerWidth
        {
            get => _tiledArrangerWidth;
            set
            {
                _tiledArrangerWidth = value;
                NotifyOfPropertyChange(() => TiledArrangerWidth);
                ResizeArranger(TiledArrangerWidth, TiledArrangerHeight);
            }
        }

        private int _tiledArrangerHeight = 16;
        public int TiledArrangerHeight
        {
            get => _tiledArrangerHeight;
            set
            {
                _tiledArrangerHeight = value;
                NotifyOfPropertyChange(() => TiledArrangerHeight);
                ResizeArranger(TiledArrangerWidth, TiledArrangerHeight);
            }
        }

        private int _linearArrangerWidth = 256;
        public int LinearArrangerWidth
        {
            get => _linearArrangerWidth;
            set
            {
                _linearArrangerWidth = value;
                NotifyOfPropertyChange(() => LinearArrangerWidth);
                ChangeCodecDimensions(LinearArrangerWidth, LinearArrangerHeight);
            }
        }

        private int _linearArrangerHeight = 256;
        public int LinearArrangerHeight
        {
            get => _linearArrangerHeight;
            set
            {
                _linearArrangerHeight = value;
                NotifyOfPropertyChange(() => LinearArrangerHeight);
                ChangeCodecDimensions(LinearArrangerWidth, LinearArrangerHeight);
            }
        }

        public SequentialArrangerEditorViewModel(SequentialArranger arranger, IEventAggregator events, ICodecService codecService)
        {
            Resource = arranger;
            _arranger = arranger;
            _events = events;
            _codecService = codecService;

            foreach (var name in codecService.GetSupportedCodecNames().OrderBy(x => x))
                CodecNames.Add(name);
            _selectedCodecName = arranger.ActiveCodec.Name;

            if(arranger.Layout == ArrangerLayout.TiledArranger)
            {
                _tiledElementWidth = arranger.ElementPixelSize.Width;
                _tiledElementHeight = arranger.ElementPixelSize.Height;
                _tiledArrangerHeight = arranger.ArrangerElementSize.Height;
                _tiledArrangerWidth = arranger.ArrangerElementSize.Width;
            }
            else if(arranger.Layout == ArrangerLayout.LinearArranger)
            {
                _linearArrangerHeight = arranger.ArrangerPixelSize.Height;
                _linearArrangerWidth = arranger.ArrangerPixelSize.Width;
            }

            MoveHome();
        }

        public void MoveByteDown() => Move(ArrangerMoveType.ByteDown);
        public void MoveByteUp() => Move(ArrangerMoveType.ByteUp);
        public void MoveRowDown() => Move(ArrangerMoveType.RowDown);
        public void MoveRowUp() => Move(ArrangerMoveType.RowUp);
        public void MoveColumnRight() => Move(ArrangerMoveType.ColRight);
        public void MoveColumnLeft() => Move(ArrangerMoveType.ColLeft);
        public void MovePageDown() => Move(ArrangerMoveType.PageDown);
        public void MovePageUp() => Move(ArrangerMoveType.PageUp);
        public void MoveHome() => Move(ArrangerMoveType.Home);
        public void MoveEnd() => Move(ArrangerMoveType.End);
        public void ExpandWidth()
        {
            if (IsTiledLayout)
                TiledArrangerWidth++;
            else
                LinearArrangerWidth += 8;
        }

        public void ExpandHeight()
        {
            if (IsTiledLayout)
                TiledArrangerHeight++;
            else
                LinearArrangerHeight += 8;
        }

        public void ShrinkWidth()
        {
            if (IsTiledLayout)
                TiledArrangerWidth = Math.Clamp(TiledArrangerWidth - 1, 1, int.MaxValue);
            else
                LinearArrangerHeight = Math.Clamp(LinearArrangerHeight - 8, 1, int.MaxValue);
        }

        public void ShrinkHeight()
        {
            if (IsTiledLayout)
                TiledArrangerHeight = Math.Clamp(TiledArrangerHeight - 1, 1, int.MaxValue);
            else
                LinearArrangerWidth = Math.Clamp(LinearArrangerWidth - 8, 1, int.MaxValue);
        }

        private void Move(ArrangerMoveType moveType)
        {
            var address = (_arranger as SequentialArranger).Move(moveType);
            _arrangerImage.Invalidate();
            _arrangerImage.Render(_arranger);
            ArrangerSource = new ImageRgba32Source(_arrangerImage.Image);

            string notifyMessage = $"File Offset: 0x{address.FileOffset:X}";
            var notifyEvent = new NotifyStatusEvent(notifyMessage, NotifyStatusDuration.Indefinite);
            _events.PublishOnUIThreadAsync(notifyEvent);
        }

        private void ResizeArranger(int arrangerWidth, int arrangerHeight)
        {
            if (arrangerWidth <= 0 || arrangerHeight <= 0)
                return;

            (_arranger as SequentialArranger).Resize(arrangerWidth, arrangerHeight);
            _arrangerImage.Invalidate();
            _arrangerImage.Render(_arranger);
            ArrangerSource = new ImageRgba32Source(_arrangerImage.Image);
        }

        private void ChangeCodec()
        {
            var codec = _codecService.CodecFactory.GetCodec(SelectedCodecName);
            if (codec.Layout == ImageMagitek.Codec.ImageLayout.Tiled)
            {
                _arranger.Resize(TiledArrangerWidth, TiledArrangerHeight);
                codec = _codecService.CodecFactory.GetCodec(SelectedCodecName, TiledElementWidth, TiledElementHeight);
                (_arranger as SequentialArranger).ChangeCodec(codec);
            }
            else if (codec.Layout == ImageMagitek.Codec.ImageLayout.Linear)
            {
                _arranger.Resize(1, 1);
                codec = _codecService.CodecFactory.GetCodec(SelectedCodecName, LinearArrangerWidth, LinearArrangerHeight);
                (_arranger as SequentialArranger).ChangeCodec(codec);
            }

            _arrangerImage.Invalidate();
            _arrangerImage.Render(_arranger);
            ArrangerSource = new ImageRgba32Source(_arrangerImage.Image);

            NotifyOfPropertyChange(() => IsTiledLayout);
            NotifyOfPropertyChange(() => IsLinearLayout);
        }

        private void ChangeCodecDimensions(int width, int height)
        {
            var codec = _codecService.CodecFactory.GetCodec(SelectedCodecName, width, height);
            (_arranger as SequentialArranger).ChangeCodec(codec);
            _arrangerImage.Invalidate();
            _arrangerImage.Render(_arranger);
            ArrangerSource = new ImageRgba32Source(_arrangerImage.Image);
        }

        public override void OnMouseDown(object sender, MouseCaptureArgs e)
        {
            //throw new NotImplementedException();
        }

        public override void OnMouseLeave(object sender, MouseCaptureArgs e)
        {
            //throw new NotImplementedException();
        }

        public override void OnMouseMove(object sender, MouseCaptureArgs e)
        {
            //throw new NotImplementedException();
        }

        public override void OnMouseUp(object sender, MouseCaptureArgs e)
        {
            //throw new NotImplementedException();
        }
    }
}
