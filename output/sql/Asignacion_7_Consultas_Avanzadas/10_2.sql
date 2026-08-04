-- EJERCICIO 10.2
SET SERVEROUTPUT ON
CREATE OR REPLACE PROCEDURE detalle_empleado
  (p_employee_id IN employees.employee_id%TYPE,
   p_apellido    OUT employees.last_name%TYPE,
   p_salario     OUT employees.salary%TYPE,
   p_fecha       OUT employees.hire_date%TYPE) IS
BEGIN
  SELECT last_name, salary, hire_date
    INTO p_apellido, p_salario, p_fecha
    FROM employees
   WHERE employee_id = p_employee_id;
END;
/

DEFINE p_employee_id = 100
DECLARE
  v_apellido employees.last_name%TYPE;
  v_salario  employees.salary%TYPE;
  v_fecha    employees.hire_date%TYPE;
BEGIN
  detalle_empleado(&p_employee_id, v_apellido, v_salario, v_fecha);
  DBMS_OUTPUT.PUT_LINE('Detalle del empleado: ' || v_apellido || ', ' ||
    TO_CHAR(v_salario, 'fm$99,999.00') || ', ' ||
    TO_CHAR(v_fecha, 'DD/MM/YYYY'));
END;
/
