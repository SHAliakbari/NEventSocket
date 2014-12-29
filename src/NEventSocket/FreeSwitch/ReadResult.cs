﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReadResult.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.FreeSwitch
{
    using System;

    public class ReadResult : ApplicationResult
    {
        public ReadResult(EventMessage eventMessage, string channelVariable) : base(eventMessage)
        {
            this.Digits = eventMessage.GetVariable(channelVariable);
            var readResult = eventMessage.GetVariable("read_result");
            this.Result = !string.IsNullOrEmpty(readResult) ? (Status)Enum.Parse(typeof(Status), readResult, true) : Status.Failure;
        }

        public enum Status
        {
            Success, 

            Timeout, 

            Failure
        }

        /// <summary>
        /// Gets a string indicating the status of the read operation, "success", "timeout" or "failure"
        /// </summary>
        public Status Result { get; private set; }

        /// <summary>
        /// Gets the digits read from the Channel.
        /// </summary>
        public string Digits { get; private set; }
    }
}