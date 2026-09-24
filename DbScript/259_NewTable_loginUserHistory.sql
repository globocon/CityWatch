GO

/****** Object:  Table [dbo].[LoginUserHistory]    Script Date: 08-10-2024 23:18:37 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[LoginUserHistory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[LoginUserId] [int] NULL,
	[LoginTime] [datetime] NULL,
	[IPAddress] [nvarchar](50) NULL,
 CONSTRAINT [PK_LoginUserHistory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO



ALTER TABLE [dbo].[GuardLogins]
ADD [IPAddress] varchar(50);