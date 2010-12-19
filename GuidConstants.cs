using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordLight
{
    static class GuidConstants
    {
		public const string WordLightPackageString = "1dbfabd9-f7c7-4f4b-b062-11a2e7ee3c54";
        public static readonly Guid WordLightPackage = new Guid(WordLightPackageString);

        public const string TextMarkerServiceString = "2dbfabd9-f7c7-4f4b-b062-11a2e7ee3c54";
        public static readonly Guid TextMarkerService = new Guid(TextMarkerServiceString);

        public const string SearchMarkerTypeString = "3dbfabd9-f7c7-4f4b-b062-11a2e7ee3c54";
        public static readonly Guid SearchMarkerType = new Guid(SearchMarkerTypeString);
    };
}
