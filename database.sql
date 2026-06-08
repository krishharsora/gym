-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: Mar 19, 2026 at 05:15 PM
-- Server version: 10.4.32-MariaDB
-- PHP Version: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `cafe_management`
--

-- --------------------------------------------------------

--
-- Table structure for table `activity_logs`
--
Create database `cafe_management` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci */;
USE `cafe_management`;
CREATE TABLE `activity_logs` (
  `log_id` int(11) NOT NULL,
  `user_id` int(11) DEFAULT NULL,
  `action` varchar(255) DEFAULT NULL,
  `entity` varchar(100) DEFAULT NULL,
  `created_at` datetime DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `activity_logs`
--

INSERT INTO `activity_logs` (`log_id`, `user_id`, `action`, `entity`, `created_at`) VALUES
(1, 1, 'Added new menu item', 'MenuItems', '2026-03-16 13:36:30'),
(2, 2, 'Updated table status', 'CafeTables', '2026-03-16 13:36:30'),
(3, 4, 'Generated bill', 'Orders', '2026-03-16 13:36:30'),
(9, 1, 'Updated reservation #2 status to Confirmed', 'Reservations', '2026-03-18 18:43:41'),
(10, 1, 'Created new reservation', 'Reservations', '2026-03-18 18:45:14'),
(11, 1, 'Cancelled reservation', 'Reservations', '2026-03-18 18:45:32');

-- --------------------------------------------------------

--
-- Table structure for table `cafe_tables`
--

CREATE TABLE `cafe_tables` (
  `table_id` int(11) NOT NULL,
  `table_number` int(11) NOT NULL,
  `capacity` int(11) DEFAULT NULL,
  `qr_code_url` varchar(255) DEFAULT NULL,
  `status` enum('Available','Reserved','Occupied') DEFAULT 'Available'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `cafe_tables`
--

INSERT INTO `cafe_tables` (`table_id`, `table_number`, `capacity`, `qr_code_url`, `status`) VALUES
(1, 1, 4, '/images/table1.jpg', 'Available'),
(2, 2, 2, '/images/table2.jpg', 'Reserved'),
(3, 3, 8, '/images/table3.jpg', 'Reserved');

-- --------------------------------------------------------

--
-- Table structure for table `customers`
--

CREATE TABLE `customers` (
  `customer_id` int(11) NOT NULL,
  `name` varchar(100) NOT NULL,
  `phone` varchar(15) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  `created_at` datetime DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `customers`
--

INSERT INTO `customers` (`customer_id`, `name`, `phone`, `email`, `created_at`) VALUES
(1, 'Amit Sharma', '9991112222', 'amit@gmail.com', '2026-03-16 13:36:30'),
(2, 'Priya Patel', '9991113333', 'priya@gmail.com', '2026-03-16 13:36:30'),
(3, 'John Doe', '9991114444', 'john@gmail.com', '2026-03-16 13:36:30'),
(13, 'krish harsora', '9727368520', 'bosslive683@gmail.com', '2026-03-18 19:04:53'),
(27, 'BOSS GAMING', '9727368520', 'ashaharsora33@gmail.com', '2026-03-19 11:31:03'),
(28, 'yo', '9727368520', '23bmiit9@gmail.com', '2026-03-19 12:56:55'),
(29, 'Cashier Staff', '9876543213', 'cashier@cafe.com', '2026-03-19 19:44:14'),
(30, 'Kitchen Staff', '9876543212', 'kitchen@cafe.com', '2026-03-19 21:30:46');

-- --------------------------------------------------------

--
-- Table structure for table `feedback`
--

CREATE TABLE `feedback` (
  `feedback_id` int(11) NOT NULL,
  `customer_id` int(11) DEFAULT NULL,
  `order_id` int(11) DEFAULT NULL,
  `rating` int(11) DEFAULT NULL,
  `comments` text DEFAULT NULL,
  `created_at` datetime DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `feedback`
--

INSERT INTO `feedback` (`feedback_id`, `customer_id`, `order_id`, `rating`, `comments`, `created_at`) VALUES
(1, 1, 1, 5, 'Great coffee and service', '2026-03-16 13:36:30'),
(2, 2, 2, 4, 'Nice cafe ambiance', '2026-03-16 13:36:30');

-- --------------------------------------------------------

--
-- Table structure for table `menu_categories`
--

CREATE TABLE `menu_categories` (
  `category_id` int(11) NOT NULL,
  `category_name` varchar(100) DEFAULT NULL,
  `description` text DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `menu_categories`
--

INSERT INTO `menu_categories` (`category_id`, `category_name`, `description`) VALUES
(1, 'Coffee', 'Hot and cold coffee'),
(2, 'Snacks', 'Light snacks'),
(3, 'Desserts', 'Sweet items'),
(5, 'pizza', '---');

-- --------------------------------------------------------

--
-- Table structure for table `menu_items`
--

CREATE TABLE `menu_items` (
  `item_id` int(11) NOT NULL,
  `category_id` int(11) DEFAULT NULL,
  `item_name` varchar(100) DEFAULT NULL,
  `description` text DEFAULT NULL,
  `price` decimal(10,2) DEFAULT NULL,
  `is_available` tinyint(1) DEFAULT 1,
  `image_url` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `menu_items`
--

INSERT INTO `menu_items` (`item_id`, `category_id`, `item_name`, `description`, `price`, `is_available`, `image_url`) VALUES
(1, 1, 'Cappuccino', 'Hot cappuccino coffee', 120.00, 1, '/images/cappuccino.jpg'),
(2, 1, 'Cold Coffee', 'Chilled coffee drink', 150.00, 1, '/images/coldcoffee.jpg'),
(3, 2, 'Veg Sandwich', 'Grilled vegetable sandwich', 90.00, 1, '/images/sandwich.jpg'),
(4, 3, 'Chocolate Cake', 'Chocolate dessert cake', 180.00, 1, '/images/cake.jpg'),
(6, 5, 'pizza', NULL, 120.00, 1, '/images/bcbb0f00-294b-45c0-af13-bee0dfcaa3e3_pizza.jpg');

-- --------------------------------------------------------

--
-- Table structure for table `orders`
--

CREATE TABLE `orders` (
  `order_id` int(11) NOT NULL,
  `customer_id` int(11) DEFAULT NULL,
  `table_id` int(11) DEFAULT NULL,
  `order_time` datetime DEFAULT current_timestamp(),
  `order_status` enum('Pending','Preparing','Ready','Served','Completed') DEFAULT 'Pending',
  `StripePaymentIntentId` varchar(255) DEFAULT NULL,
  `total_amount` decimal(10,2) DEFAULT NULL,
  `StripeSessionId` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `orders`
--

INSERT INTO `orders` (`order_id`, `customer_id`, `table_id`, `order_time`, `order_status`, `StripePaymentIntentId`, `total_amount`, `StripeSessionId`) VALUES
(1, 1, 1, '2026-03-16 13:36:30', 'Completed', NULL, 270.00, NULL),
(2, 2, 2, '2026-03-16 13:36:30', 'Completed', NULL, 150.00, NULL),
(21, 13, NULL, '2026-03-19 06:40:17', 'Served', 'pi_3TCaFfQuyAg2bqww0BtbkFVj', 150.00, 'cs_test_a1Rp1auBOWh9NSn82Cg49bDVtboJUjkqdp3gcr033mZJqKbue8EtwQDIhm'),
(22, 13, NULL, '2026-03-19 06:42:11', '', 'pi_3TCaHVQuyAg2bqww0brFnPZh', 120.00, 'cs_test_a1Ui21obYP6W6KIkK3UaBYa2XETxRYkMe4X4sm26maj2TC72gpU7LFg8Tv'),
(23, 13, NULL, '2026-03-19 07:22:47', '', 'pi_3TCaumQuyAg2bqww0Wepr4Gd', 120.00, 'cs_test_a188aHHPj9j9QfurUOVNPTWdAuXlET8TZWDxytJW9ubSkap3PagLcmj3s3'),
(24, 13, NULL, '2026-03-19 07:47:59', '', 'pi_3TCbJBQuyAg2bqww1rFs6ZqV', 150.00, 'cs_test_a1M8Yk3i3hknSYoZzzztR0CGFHOtJf4InlwRYptOBWxfU35kKVmRgqTnwR'),
(25, 27, NULL, '2026-03-19 14:12:43', 'Completed', 'pi_3TChJVQuyAg2bqww0rHSrqNr', 240.00, 'cs_test_a1A2nTYqaF0SwZhoyuwAFf3N72AkitZ8vZ9unD2IQkYrkyOvgU7ZiF0g2T'),
(26, 27, NULL, '2026-03-19 16:07:43', 'Pending', 'pi_3TCj6mQuyAg2bqww08yTqGhx', 150.00, 'cs_test_a1GTuB5yxLhAUSrQ25TN4eeof5rldPOMG8ebzCsGfPi9pp6MqdsfZyLpBc');

-- --------------------------------------------------------

--
-- Table structure for table `order_items`
--

CREATE TABLE `order_items` (
  `order_item_id` int(11) NOT NULL,
  `order_id` int(11) DEFAULT NULL,
  `item_id` int(11) DEFAULT NULL,
  `quantity` int(11) DEFAULT NULL,
  `price` decimal(10,2) DEFAULT NULL,
  `subtotal` decimal(10,2) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `order_items`
--

INSERT INTO `order_items` (`order_item_id`, `order_id`, `item_id`, `quantity`, `price`, `subtotal`) VALUES
(1, 1, 1, 1, 120.00, 120.00),
(2, 1, 3, 1, 90.00, 90.00),
(3, 1, 4, 1, 60.00, 60.00),
(4, 2, 2, 1, 150.00, 150.00),
(35, 21, 2, 1, 150.00, 150.00),
(36, 22, 1, 1, 120.00, 120.00),
(37, 23, 1, 1, 120.00, 120.00),
(38, 24, 2, 1, 150.00, 150.00),
(39, 25, 2, 1, 150.00, 150.00),
(40, 25, 3, 1, 90.00, 90.00),
(41, 26, 2, 1, 150.00, 150.00);

-- --------------------------------------------------------

--
-- Table structure for table `payments`
--

CREATE TABLE `payments` (
  `payment_id` int(11) NOT NULL,
  `order_id` int(11) DEFAULT NULL,
  `StripeSessionId` varchar(255) DEFAULT NULL,
  `payment_method` enum('Cash','UPI','Stripe') DEFAULT NULL,
  `amount` decimal(10,2) DEFAULT NULL,
  `payment_status` enum('Pending','Paid') DEFAULT 'Pending',
  `paid_at` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `payments`
--

INSERT INTO `payments` (`payment_id`, `order_id`, `StripeSessionId`, `payment_method`, `amount`, `payment_status`, `paid_at`) VALUES
(1, 1, NULL, 'UPI', 270.00, 'Paid', '2026-03-16 13:36:30'),
(2, 2, NULL, 'UPI', 150.00, 'Paid', '2026-03-18 12:20:08'),
(14, 21, 'cs_test_a1Rp1auBOWh9NSn82Cg49bDVtboJUjkqdp3gcr033mZJqKbue8EtwQDIhm', 'Stripe', 150.00, 'Paid', '2026-03-19 06:40:18'),
(15, 22, 'cs_test_a1Ui21obYP6W6KIkK3UaBYa2XETxRYkMe4X4sm26maj2TC72gpU7LFg8Tv', 'Stripe', 120.00, 'Paid', '2026-03-19 06:42:11'),
(16, 23, 'cs_test_a188aHHPj9j9QfurUOVNPTWdAuXlET8TZWDxytJW9ubSkap3PagLcmj3s3', 'Stripe', 120.00, 'Paid', '2026-03-19 07:22:47'),
(17, 24, 'cs_test_a1M8Yk3i3hknSYoZzzztR0CGFHOtJf4InlwRYptOBWxfU35kKVmRgqTnwR', 'Stripe', 150.00, 'Paid', '2026-03-19 07:48:00'),
(18, 25, 'cs_test_a1A2nTYqaF0SwZhoyuwAFf3N72AkitZ8vZ9unD2IQkYrkyOvgU7ZiF0g2T', 'Stripe', 240.00, 'Paid', '2026-03-19 14:12:43'),
(19, 26, 'cs_test_a1GTuB5yxLhAUSrQ25TN4eeof5rldPOMG8ebzCsGfPi9pp6MqdsfZyLpBc', 'Stripe', 150.00, 'Paid', '2026-03-19 16:07:43');

-- --------------------------------------------------------

--
-- Table structure for table `reservations`
--

CREATE TABLE `reservations` (
  `reservation_id` int(11) NOT NULL,
  `customer_id` int(11) DEFAULT NULL,
  `table_id` int(11) DEFAULT NULL,
  `reservation_date` date DEFAULT NULL,
  `reservation_time` time DEFAULT NULL,
  `guest_count` int(11) DEFAULT NULL,
  `status` enum('Pending','Confirmed','Cancelled') DEFAULT 'Pending'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `reservations`
--

INSERT INTO `reservations` (`reservation_id`, `customer_id`, `table_id`, `reservation_date`, `reservation_time`, `guest_count`, `status`) VALUES
(1, 1, 3, '2026-04-10', '19:00:00', 4, 'Confirmed'),
(2, 2, 2, '2026-04-11', '18:30:00', 2, 'Confirmed'),
(3, 3, 1, '2026-04-10', '19:00:00', 4, 'Cancelled');

-- --------------------------------------------------------

--
-- Table structure for table `users`
--

CREATE TABLE `users` (
  `user_id` int(11) NOT NULL,
  `full_name` varchar(100) NOT NULL,
  `email` varchar(100) DEFAULT NULL,
  `phone` varchar(15) DEFAULT NULL,
  `role` enum('Admin','Manager','Kitchen','Cashier','Customer') NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `status` tinyint(1) DEFAULT 1,
  `created_at` datetime DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `users`
--

INSERT INTO `users` (`user_id`, `full_name`, `email`, `phone`, `role`, `password_hash`, `status`, `created_at`) VALUES
(1, 'krish harsora', '23bmiit169@gmail.com', '9874563210', 'Admin', 'AQAAAAIAAYagAAAAEHsWBQD6VV99sIkT6TBI1ZwJi3mZu7tvJxUucCZOP2CvUl7wJw+bKDd38RfjwOJIjQ==', 1, '2026-03-15 22:16:17'),
(3, 'Admin User', 'admin@cafe.com', '9876543210', 'Admin', 'AQAAAAIAAYagAAAAEKVkD2WRhh7Nynf/QN/2G1lCMWkZIU2mVxaZjulQtvGndoWrihCuv2bLtsn1XRq+HA==', 1, '2026-03-16 13:36:30'),
(4, 'Rahul Manager', 'manager@cafe.com', '9876543211', 'Manager', 'AQAAAAIAAYagAAAAEKVkD2WRhh7Nynf/QN/2G1lCMWkZIU2mVxaZjulQtvGndoWrihCuv2bLtsn1XRq+HA==', 1, '2026-03-16 13:36:30'),
(5, 'Kitchen Staff', 'kitchen@cafe.com', '9876543212', 'Kitchen', 'AQAAAAIAAYagAAAAEKVkD2WRhh7Nynf/QN/2G1lCMWkZIU2mVxaZjulQtvGndoWrihCuv2bLtsn1XRq+HA==', 1, '2026-03-16 13:36:30'),
(6, 'Cashier Staff', 'cashier@cafe.com', '9876543213', 'Cashier', 'AQAAAAIAAYagAAAAEKVkD2WRhh7Nynf/QN/2G1lCMWkZIU2mVxaZjulQtvGndoWrihCuv2bLtsn1XRq+HA==', 1, '2026-03-16 13:36:30'),
(7, 'meet', '23bmiit1@gmail.com', '09727368520', 'Admin', 'AQAAAAIAAYagAAAAEKVkD2WRhh7Nynf/QN/2G1lCMWkZIU2mVxaZjulQtvGndoWrihCuv2bLtsn1XRq+HA==', 1, '2026-03-17 11:35:03'),
(9, 'krish harsora', 'bosslive683@gmail.com', '9727368520', 'Customer', 'AQAAAAIAAYagAAAAEJrRmln3+ESI0fO4WDAaVXsH7iiIPDdDMqnFZN2oy8TZt4GCw45JGwfZ6o1f/kiMXw==', 1, '2026-03-18 19:04:53'),
(10, 'BOSS GAMING', 'ashaharsora33@gmail.com', '9727368520', 'Customer', 'AQAAAAIAAYagAAAAEF8w15MuK2U71aT97v6d1Jsfnjmlf9EkNz7/eVhFznXkHJfuKn1tTjqtXB6CfB7ITA==', 1, '2026-03-19 11:31:03'),
(11, 'yo', '23bmiit9@gmail.com', '9727368520', 'Customer', 'AQAAAAIAAYagAAAAEHsWBQD6VV99sIkT6TBI1ZwJi3mZu7tvJxUucCZOP2CvUl7wJw+bKDd38RfjwOJIjQ==', 1, '2026-03-19 12:56:04');

--
-- Indexes for dumped tables
--

--
-- Indexes for table `activity_logs`
--
ALTER TABLE `activity_logs`
  ADD PRIMARY KEY (`log_id`),
  ADD KEY `user_id` (`user_id`);

--
-- Indexes for table `cafe_tables`
--
ALTER TABLE `cafe_tables`
  ADD PRIMARY KEY (`table_id`);

--
-- Indexes for table `customers`
--
ALTER TABLE `customers`
  ADD PRIMARY KEY (`customer_id`);

--
-- Indexes for table `feedback`
--
ALTER TABLE `feedback`
  ADD PRIMARY KEY (`feedback_id`),
  ADD KEY `customer_id` (`customer_id`),
  ADD KEY `order_id` (`order_id`);

--
-- Indexes for table `menu_categories`
--
ALTER TABLE `menu_categories`
  ADD PRIMARY KEY (`category_id`);

--
-- Indexes for table `menu_items`
--
ALTER TABLE `menu_items`
  ADD PRIMARY KEY (`item_id`),
  ADD KEY `category_id` (`category_id`);

--
-- Indexes for table `orders`
--
ALTER TABLE `orders`
  ADD PRIMARY KEY (`order_id`),
  ADD KEY `customer_id` (`customer_id`),
  ADD KEY `table_id` (`table_id`);

--
-- Indexes for table `order_items`
--
ALTER TABLE `order_items`
  ADD PRIMARY KEY (`order_item_id`),
  ADD KEY `order_id` (`order_id`),
  ADD KEY `item_id` (`item_id`);

--
-- Indexes for table `payments`
--
ALTER TABLE `payments`
  ADD PRIMARY KEY (`payment_id`),
  ADD KEY `order_id` (`order_id`);

--
-- Indexes for table `reservations`
--
ALTER TABLE `reservations`
  ADD PRIMARY KEY (`reservation_id`),
  ADD KEY `customer_id` (`customer_id`),
  ADD KEY `table_id` (`table_id`);

--
-- Indexes for table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`user_id`),
  ADD UNIQUE KEY `email` (`email`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `activity_logs`
--
ALTER TABLE `activity_logs`
  MODIFY `log_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=15;

--
-- AUTO_INCREMENT for table `cafe_tables`
--
ALTER TABLE `cafe_tables`
  MODIFY `table_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- AUTO_INCREMENT for table `customers`
--
ALTER TABLE `customers`
  MODIFY `customer_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=31;

--
-- AUTO_INCREMENT for table `feedback`
--
ALTER TABLE `feedback`
  MODIFY `feedback_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT for table `menu_categories`
--
ALTER TABLE `menu_categories`
  MODIFY `category_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT for table `menu_items`
--
ALTER TABLE `menu_items`
  MODIFY `item_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=7;

--
-- AUTO_INCREMENT for table `orders`
--
ALTER TABLE `orders`
  MODIFY `order_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=27;

--
-- AUTO_INCREMENT for table `order_items`
--
ALTER TABLE `order_items`
  MODIFY `order_item_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=42;

--
-- AUTO_INCREMENT for table `payments`
--
ALTER TABLE `payments`
  MODIFY `payment_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=20;

--
-- AUTO_INCREMENT for table `reservations`
--
ALTER TABLE `reservations`
  MODIFY `reservation_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT for table `users`
--
ALTER TABLE `users`
  MODIFY `user_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=12;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `activity_logs`
--
ALTER TABLE `activity_logs`
  ADD CONSTRAINT `activity_logs_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`);

--
-- Constraints for table `feedback`
--
ALTER TABLE `feedback`
  ADD CONSTRAINT `feedback_ibfk_1` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`customer_id`),
  ADD CONSTRAINT `feedback_ibfk_2` FOREIGN KEY (`order_id`) REFERENCES `orders` (`order_id`);

--
-- Constraints for table `menu_items`
--
ALTER TABLE `menu_items`
  ADD CONSTRAINT `menu_items_ibfk_1` FOREIGN KEY (`category_id`) REFERENCES `menu_categories` (`category_id`);

--
-- Constraints for table `orders`
--
ALTER TABLE `orders`
  ADD CONSTRAINT `orders_ibfk_1` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`customer_id`),
  ADD CONSTRAINT `orders_ibfk_2` FOREIGN KEY (`table_id`) REFERENCES `cafe_tables` (`table_id`);

--
-- Constraints for table `order_items`
--
ALTER TABLE `order_items`
  ADD CONSTRAINT `order_items_ibfk_1` FOREIGN KEY (`order_id`) REFERENCES `orders` (`order_id`),
  ADD CONSTRAINT `order_items_ibfk_2` FOREIGN KEY (`item_id`) REFERENCES `menu_items` (`item_id`);

--
-- Constraints for table `payments`
--
ALTER TABLE `payments`
  ADD CONSTRAINT `payments_ibfk_1` FOREIGN KEY (`order_id`) REFERENCES `orders` (`order_id`);

--
-- Constraints for table `reservations`
--
ALTER TABLE `reservations`
  ADD CONSTRAINT `reservations_ibfk_1` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`customer_id`),
  ADD CONSTRAINT `reservations_ibfk_2` FOREIGN KEY (`table_id`) REFERENCES `cafe_tables` (`table_id`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
