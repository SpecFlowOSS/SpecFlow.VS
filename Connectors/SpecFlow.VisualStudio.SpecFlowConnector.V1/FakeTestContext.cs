﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Data;
using System.Data.Common;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery
{
    class FakeTestContext : TestContext
    {
        public override IDictionary Properties => throw new NotImplementedException();

        public override DataRow DataRow => throw new NotImplementedException();

        public override DbConnection DataConnection => throw new NotImplementedException();

        public override void AddResultFile(string fileName)
        {
            throw new NotImplementedException();
        }

        public override void BeginTimer(string timerName)
        {
            throw new NotImplementedException();
        }

        public override void EndTimer(string timerName)
        {
            throw new NotImplementedException();
        }

        public override void Write(string message)
        {
            throw new NotImplementedException();
        }

        public override void Write(string format, params object[] args)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(string message)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(string format, params object[] args)
        {
            throw new NotImplementedException();
        }
    }
}
