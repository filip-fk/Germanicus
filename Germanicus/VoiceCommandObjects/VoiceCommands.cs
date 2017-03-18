﻿//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Object for passing voice-command arguments to the app 
/// and execute the action requested by the user. 
/// See VoiceCommands.xml and OnActivated() in App.xaml.cs.
/// </summary>
namespace Germanicus.VoiceCommandObjects
{
    public class VoiceCommand
    {
        /// <summary>
        /// The voice command (Name attribute of Command element in VoiceCommands.xml).
        /// </summary>
        public string VoiceCommandName { get; set; }

        /// <summary>
        /// The command mode (voice or text activation).
        /// </summary>
        public string CommandMode { get; set; }

        /// <summary>
        /// The raw voice command text.
        /// </summary>
        public string TextSpoken { get; set; }

        /// <summary>
        /// The note target.
        /// </summary>
        public string NoteOwner { get; set; }
    }
}
