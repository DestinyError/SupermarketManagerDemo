
SELECT * FROM users;

create table users (
	username varchar(32) not null primary key,
	password varchar(64) not null,
	user_type int not null,
	date_created DateTime not null,
	constraint CK_user_type check (user_type >= 0 and user_type <= 1)
);

drop table users;

INSERT INTO users VALUES ('admin', 'pass', 0, GETDATE());
INSERT INTO users VALUES ('user1', 'pass1', 1, GETDATE());
INSERT INTO users VALUES ('user2', 'pass2', 1, GETDATE());
INSERT INTO users VALUES ('user3', 'pass3', 1, GETDATE());

delete from users;

select * from users where user_type = 1


///////////////

create function dbo.CheckUnit(@unit varchar(32))
returns bit as
begin
	if exists(select * from units where units.name = @unit)
	begin
		return 1
	end
	return 0
end

drop function dbo.CheckUnit


create table items (
	id varchar(32) not null primary key,
	name nvarchar(512) not null,
	price float not null,
	unit varchar(32) not null,
	constraint CK_price check (price >= 0),
	constraint CK_unit check (dbo.CheckUnit(unit) = 1)
);

insert into items values('food001', 'Fuji apple', 6.8, 'bag');
insert into items values('food002', 'Cheese hotdog', 1.2, 'packet');
insert into items values('food003', 'Frozen salmon', 11.5, 'kg');
insert into items values('food004', 'Aquafina 500ml', 1, 'bottle');

select * from items

drop table items;

//////////////

create table in_stock (
	item_id varchar(32) not null primary key foreign key references items(id),
	quantity float not null,
	constraint CK_quantity check(quantity >= 0)
);

insert into in_stock values ('food001', 250)
insert into in_stock values ('food002', 120)
insert into in_stock values ('food003', 70)
insert into in_stock values ('food004', 300)

select * from in_stock

drop table in_stock

/////////////

create table units (
	name varchar(32) not null primary key
);

insert into units(name) values ('unit'), ('kg'), ('100gr'), ('piece'), ('liter'), ('packet'), ('carton'), ('dozen'), ('bar'), ('bag'), ('bottle'), ('box');

select * from units

drop table units

delete from units where name = 'Can'

//////////////


select items.name, items.price, items.unit, in_stock.quantity
from items
left join in_stock on items.id = in_stock.item_id

select items.name, items.price, items.unit, in_stock.quantity
from items, in_stock
where items.id = in_stock.item_id


//////////

update in_stock
set quantity = quantity + 10 where item_id = 'food001'


select * from in_stock

///////////

create table items_log (
	log_id int identity(1, 1) primary key not null,
	item_id varchar(32) foreign key references items(id) not null,
	change_amount float not null,
	date_time DateTime not null,
	user varchar(32) foreign key references users(username) not null
)