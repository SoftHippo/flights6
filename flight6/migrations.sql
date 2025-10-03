BEGIN TRANSACTION;

CREATE TABLE airlines (
    IataCode NVARCHAR(10) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    ThreeDigitCode NVARCHAR(10) NOT NULL,
    IcaoCode NVARCHAR(10) NOT NULL,
    Country NVARCHAR(100) NOT NULL
);

CREATE TABLE airports (
    IataCode NVARCHAR(10) PRIMARY KEY,
    City NVARCHAR(100) NOT NULL,
    Country NVARCHAR(100) NOT NULL
);

CREATE TABLE aircraft_types (
    IcaoCode NVARCHAR(10) PRIMARY KEY CHECK (IcaoCode <> ''),
    Name NVARCHAR(255) NOT NULL CHECK (Name <> '')
);

CREATE TABLE aircraft (
    Tail NVARCHAR(20) PRIMARY KEY CHECK (Tail <> ''),
    IcaoCode NVARCHAR(10) NOT NULL,
    SerialNumber NVARCHAR(50) CHECK (SerialNumber <> ''),
    Year INT,
    Month INT CHECK (Month BETWEEN 1 AND 12),
    ModeS NVARCHAR(20) CHECK (ModeS <> ''),
    Model NVARCHAR(100) CHECK (Model <> ''),
    Notes NVARCHAR(MAX) CHECK (Notes <> ''),
    FOREIGN KEY (IcaoCode) REFERENCES aircraft_types(IcaoCode)
);

CREATE TABLE flights5 (
    Id INT PRIMARY KEY,
    Date DATE NOT NULL,
    Airline NVARCHAR(10) NOT NULL,
    FlightNumber NVARCHAR(20) NOT NULL CHECK (FlightNumber <> ''),
    AircraftType NVARCHAR(10) NOT NULL,
    Tail NVARCHAR(20),
    Origin NVARCHAR(10) NOT NULL,
    Destination NVARCHAR(10) NOT NULL,
    Class CHAR(1) NOT NULL CHECK (Class IN ('e', 'f', 'b', 'l', 'p')),
    Seat NVARCHAR(10) CHECK (Seat <> ''),
    Terminal NVARCHAR(10) CHECK (Terminal <> ''),
    Gate NVARCHAR(10) CHECK (Gate <> ''),
    Notes NVARCHAR(MAX) CHECK (Notes <> ''),
    CONSTRAINT UQ_Flight UNIQUE (Date, Airline, FlightNumber),
    FOREIGN KEY (Airline) REFERENCES airlines(IataCode),
    FOREIGN KEY (AircraftType) REFERENCES aircraft_types(IcaoCode),
    FOREIGN KEY (Tail) REFERENCES aircraft(Tail),
    FOREIGN KEY (Origin) REFERENCES airports(IataCode),
    FOREIGN KEY (Destination) REFERENCES airports(IataCode)
);

COMMIT;

begin transaction;
ALTER TABLE Aircraft
ADD CONSTRAINT chk_IcaoCode_not_blank
CHECK (LTRIM(RTRIM(IcaoCode)) <> '');
commit;

BEGIN TRANSACTION;
ALTER TABLE flights5 ADD DayOrder INT;
commit;


BEGIN TRANSACTION;
ALTER TABLE flights5 DROP CONSTRAINT PK_flights5;

ALTER TABLE flights5 ADD CONSTRAINT PK_flights5 PRIMARY KEY ([Date], Airline, FlightNumber, Origin, Destination);

commit;


BEGIN TRANSACTION;
insert into dbo.airlines (IataCode, Name, IcaoCode, ThreeDigitCode, Country)
values ('CO', 'Continental Airlines', 'COA', '005', 'United States'),
('US', 'US Airways', 'USA', '037', 'United States');
commit;

BEGIN TRANSACTION;
ALTER TABLE dbo.flights5
ALTER COLUMN AircraftType NVARCHAR(10) NULL;
commit;

BEGIN TRANSACTION;
ALTER TABLE dbo.flights5
ALTER COLUMN Terminal NVARCHAR(50) NULL;
commit;

BEGIN TRANSACTION;
-- ADO, EZY, JNA, ASV, JSA, SWE, AWE

update dbo.airlines set IcaoCode = 'ADO', Name = 'AIRDO Co., Ltd.' where IataCode = 'HD';
update dbo.airlines set IcaoCode = 'EZY', Name = 'EasyJet plc' where IataCode = 'U2';
update dbo.airlines set IcaoCode = 'JNA', Name = 'Jin Air Co., Ltd.' where IataCode = 'LJ';
update dbo.airlines set IcaoCode = 'ASV', Name = 'Air Seoul' where IataCode = 'RS';
update dbo.airlines set IcaoCode = 'JSA', Name = 'Jetstar Asia' where IataCode = '3K';
update dbo.airlines set IcaoCode = 'SWA', Name = 'Southwest Airlines Co.' where IataCode = 'WN';
update dbo.airlines set IcaoCode = 'AWE', Name = 'US Airways' where IataCode = 'US';

commit;

BEGIN TRANSACTION;
update dbo.airlines set IcaoCode = 'GWI', name = 'Germanwings GmbH' where IataCode = '4U';
commit;
