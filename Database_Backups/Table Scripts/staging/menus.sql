USE [DB_51435_menudart]
GO
/****** Object:  Table [dbo].[Menus]    Script Date: 12/29/2012 15:02:45 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Menus_staging](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[City] [nvarchar](max) NOT NULL,
	[Active] [bit] NOT NULL,
	[ChangesUnpublished] [bit] NOT NULL,
	[Website] [nvarchar](max) NULL,
	[AboutTitle] [nvarchar](max) NULL,
	[AboutText] [nvarchar](max) NULL,
	[Template] [nvarchar](max) NULL,
	[Owner] [nvarchar](max) NULL,
	[MenuDartUrl] [nvarchar](max) NULL,
	[PreviousMenuDartUrl] [nvarchar](max) NULL,
	[Phone] [nvarchar](max) NULL,
	[Email] [nvarchar](max) NULL,
	[Facebook] [nvarchar](max) NULL,
	[Twitter] [nvarchar](max) NULL,
	[Yelp] [nvarchar](max) NULL,
	[Locations] [xml] NULL,
	[MenuTree] [xml] NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
